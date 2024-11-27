namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NServiceBus.MessageMutator;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        protected FunctionEndpointComponent(TransportTransactionMode transactionMode = TransportTransactionMode.ReceiveOnly) =>
            sendsAtomicWithReceive = transactionMode switch
            {
                TransportTransactionMode.SendsAtomicWithReceive => true,
                TransportTransactionMode.ReceiveOnly => false,
                TransportTransactionMode.None => throw new Exception("Unsupported transaction mode " + transactionMode),
                TransportTransactionMode.TransactionScope => throw new Exception("Unsupported transaction mode " + transactionMode),
                _ => throw new Exception("Unsupported transaction mode " + transactionMode),
            };

        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor) =>
            Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    Messages,
                    CustomizeConfiguration,
                    OnStartCore,
                    runDescriptor.ScenarioContext,
                    GetType(),
                    DoNotFailOnErrorMessages,
                    TypesScopedByTestClassAssemblyScanningEnabled,
                    sendsAtomicWithReceive,
                    ServiceBusMessageActionsFactory));

        protected IList<object> Messages { get; } = [];

        protected bool DoNotFailOnErrorMessages { get; init; }

        protected bool TypesScopedByTestClassAssemblyScanningEnabled { get; init; } = true;

        protected Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> ServiceBusMessageActionsFactory { get; set; } = (r, _) => new TestableServiceBusMessageActions(r);

        protected Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; init; } = _ => { };

        protected virtual Task OnStart(IFunctionEndpoint functionEndpoint, ExecutionContext executionContext) => Task.CompletedTask;

        Task OnStartCore(IFunctionEndpoint functionEndpoint, ExecutionContext executionContext) => OnStart(functionEndpoint, executionContext);

        readonly bool sendsAtomicWithReceive;

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(IList<object> messages,
                Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                Func<IFunctionEndpoint, ExecutionContext, Task> onStart,
                ScenarioContext scenarioContext,
                Type functionComponentType,
                bool doNotFailOnErrorMessages,
                bool typesScopedByTestClassAssemblyScanningEnabled,
                bool sendsAtomicWithReceive,
                Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory)
            {
                this.messages = messages;
                this.configurationCustomization = configurationCustomization;
                this.onStart = onStart;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                this.typesScopedByTestClassAssemblyScanningEnabled = typesScopedByTestClassAssemblyScanningEnabled;
                this.doNotFailOnErrorMessages = doNotFailOnErrorMessages;
                this.sendsAtomicWithReceive = sendsAtomicWithReceive;
                this.serviceBusMessageActionsFactory = serviceBusMessageActionsFactory;

                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override async Task Start(CancellationToken cancellationToken = default)
            {
                var hostBuilder = new FunctionHostBuilder();
                hostBuilder.UseNServiceBus(Name, triggerConfiguration =>
                {
                    var endpointConfiguration = triggerConfiguration.AdvancedConfiguration;

                    if (typesScopedByTestClassAssemblyScanningEnabled)
                    {
                        endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());
                    }

                    endpointConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0))
                        .Delayed(d => d.NumberOfRetries(0))
                        .Failed(c => c
                            // track messages sent to the error queue to fail the test
                            .OnMessageSentToErrorQueue((failedMessage, _) =>
                            {
                                scenarioContext.FailedMessages.AddOrUpdate(
                                    Name,
                                    new[] { failedMessage },
                                    (_, fm) =>
                                    {
                                        var messages = fm.ToList();
                                        messages.Add(failedMessage);
                                        return messages;
                                    });
                                return Task.CompletedTask;
                            }));


                    endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                    endpointConfiguration.RegisterComponents(c => c.AddSingleton<IMutateOutgoingTransportMessages>(b => new TestIndependenceMutator(scenarioContext)));

                    configurationCustomization(triggerConfiguration);
                });

                host = hostBuilder.Build();
                await host.StartAsync(cancellationToken);

                endpoint = host.Services.GetRequiredService<IFunctionEndpoint>();
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
            {
                await onStart(endpoint, new ExecutionContext());

                if (messages.Count == 0)
                {
                    return;
                }

                var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);

                var client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
                {
                    EnableCrossEntityTransactions = sendsAtomicWithReceive
                });
                var serviceBusAdministrationClient = new ServiceBusAdministrationClient(connectionString);
                var functionInputQueueName = Name;

                if (!await serviceBusAdministrationClient.QueueExistsAsync(functionInputQueueName, cancellationToken))
                {
                    await serviceBusAdministrationClient.CreateQueueAsync(functionInputQueueName, cancellationToken);
                }

                var sender = client.CreateSender(functionInputQueueName);

                foreach (var message in messages)
                {
                    var messageId = Guid.NewGuid().ToString("N");

                    var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message))
                    {
                        MessageId = messageId,
                        ApplicationProperties =
                        {
                            ["NServiceBus.EnclosedMessageTypes"] = message.GetType().FullName
                        }
                    };

                    await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

                    var receiver = client.CreateReceiver(functionInputQueueName);

                    IReadOnlyList<ServiceBusReceivedMessage> receivedMessages;
                    do
                    {
                        receivedMessages = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);

                        foreach (var receivedMessage in receivedMessages)
                        {
                            if (receivedMessage.MessageId != messageId)
                            {
                                await receiver.CompleteMessageAsync(receivedMessage, cancellationToken);
                                continue;
                            }

                            if (sendsAtomicWithReceive)
                            {
                                await endpoint.ProcessAtomic(receivedMessage, new ExecutionContext(), client,
                                    serviceBusMessageActionsFactory(receiver, scenarioContext), null,
                                    cancellationToken);
                            }
                            else
                            {
                                try
                                {
                                    await endpoint.ProcessNonAtomic(receivedMessage, new ExecutionContext(), null,
                                        cancellationToken);
                                    await receiver.CompleteMessageAsync(receivedMessage, cancellationToken);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                                {
                                    await receiver.AbandonMessageAsync(receivedMessage,
                                        cancellationToken: cancellationToken);
                                    if (!doNotFailOnErrorMessages)
                                    {
                                        throw;

                                    }
                                }
                            }
                        }
                    } while (receivedMessages.Count > 0);
                }
                if (!doNotFailOnErrorMessages)
                {
                    if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                    {
                        throw new MessageFailedException(failedMessages.First(), scenarioContext);
                    }
                }
            }

            public override async Task Stop(CancellationToken cancellationToken = default)
            {
                try
                {
                    await host.StopAsync(cancellationToken);

                    if (!doNotFailOnErrorMessages)
                    {
                        if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                        {
                            throw new MessageFailedException(failedMessages.First(), scenarioContext);
                        }
                    }
                }
                finally
                {
                    host.Dispose();
                }
            }

            // There is some non-trivial hackery going on in order to bypass the azure function host assumptions. In order to
            // simulate a similar environment we have to use a host builder so that we also get the possibility to run
            // hosted services etc. But the function hosts requires an already initialized service collection early on
            // but the host builder used by functions is still using the lambda based approach. To work around this we
            // have to forward the service registrations to the host builder and some other things manually. This is not
            // great but once the functions host moved to the new host builder this can be simplified.
            sealed class FunctionHostBuilder : IFunctionsHostBuilder, IFunctionsHostBuilderExt
            {
                HostBuilderContext context;
                readonly HostBuilder hostBuilder = new();

                public IServiceCollection Services { get; } = new ServiceCollection();

                public FunctionsHostBuilderContext Context
                {
                    get
                    {
                        if (context != null)
                        {
                            return context;
                        }

                        var configurationBuilder = new ConfigurationBuilder();
                        configurationBuilder.AddEnvironmentVariables();
                        var configurationRoot = configurationBuilder.Build();
                        context = new HostBuilderContext(new WebJobsBuilderContext { Configuration = configurationRoot, ApplicationRootPath = AppDomain.CurrentDomain.BaseDirectory });
                        return context;
                    }
                }

                public IHost Build()
                {
                    hostBuilder.ConfigureHostConfiguration(configuration =>
                    {
                        configuration.AddEnvironmentVariables();
                    });
                    // Forwarding all the service registrations to the host builder
                    hostBuilder.ConfigureServices(services =>
                    {
                        services.AddHostedService<InitializationHost>();
                        foreach (var service in Services)
                        {
                            services.Add(service);
                        }
                        Services.Clear();
                    });
                    return hostBuilder.Build();
                }

                sealed class HostBuilderContext : FunctionsHostBuilderContext
                {
                    public HostBuilderContext(WebJobsBuilderContext webJobsBuilderContext) : base(webJobsBuilderContext)
                    {
                    }
                }
            }

            IList<object> messages;
            IHost host;
            IFunctionEndpoint endpoint;

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly Func<IFunctionEndpoint, ExecutionContext, Task> onStart;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            readonly bool typesScopedByTestClassAssemblyScanningEnabled;
            readonly bool doNotFailOnErrorMessages;
            readonly bool sendsAtomicWithReceive;
            readonly Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory;
        }
    }
}