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
                    sendsAtomicWithReceive,
                    ServiceBusMessageActionsFactory));

        public IList<object> Messages { get; } = new List<object>();

        public bool DoNotFailOnErrorMessages { get; set; }

        public Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> ServiceBusMessageActionsFactory { get; set; } = (r, _) => new TestableServiceBusMessageActions(r);

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

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
                bool sendsAtomicWithReceive,
                Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory)
            {
                this.messages = messages;
                this.configurationCustomization = configurationCustomization;
                this.onStart = onStart;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                this.doNotFailOnErrorMessages = doNotFailOnErrorMessages;
                this.sendsAtomicWithReceive = sendsAtomicWithReceive;
                this.serviceBusMessageActionsFactory = serviceBusMessageActionsFactory;

                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override Task Start(CancellationToken cancellationToken = default)
            {
                var hostBuilder = new FunctionHostBuilder();
                hostBuilder.UseNServiceBus(Name, triggerConfiguration =>
                {
                    var endpointConfiguration = triggerConfiguration.AdvancedConfiguration;

                    endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

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

                serviceProvider = hostBuilder.Build();
                endpoint = serviceProvider.GetRequiredService<IFunctionEndpoint>();

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
            {
                await onStart(endpoint, new ExecutionContext());

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
                                catch (Exception)
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

            public override async Task Stop()
            {
                await serviceProvider.DisposeAsync();

                if (!doNotFailOnErrorMessages)
                {
                    if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                    {
                        throw new MessageFailedException(failedMessages.First(), scenarioContext);
                    }
                }
            }

            // There is some non-trivial hackery going on in order to bypass the azure function host assumptions. Unfortunately
            // it is not possible to use the hostbuilder here directly because functions is still using the old hostbuilder
            // that uses lambdas, and we need to be able to forward the service collection to the infrastructure. The consequence
            // of this is that some default things the host would normally provide like loading configuration from environment
            // variables, registering configuration and more needs to be done manually.
            sealed class FunctionHostBuilder : IFunctionsHostBuilder, IFunctionsHostBuilderExt
            {
                HostBuilderContext context;

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
                        Services.AddSingleton<IConfiguration>(configurationRoot);
                        context = new HostBuilderContext(new WebJobsBuilderContext { Configuration = configurationRoot, ApplicationRootPath = AppDomain.CurrentDomain.BaseDirectory });
                        return context;
                    }
                }

                public ServiceProvider Build() => Services.BuildServiceProvider();

                sealed class HostBuilderContext : FunctionsHostBuilderContext
                {
                    public HostBuilderContext(WebJobsBuilderContext webJobsBuilderContext) : base(webJobsBuilderContext)
                    {
                    }
                }
            }

            IList<object> messages;
            ServiceProvider serviceProvider;
            IFunctionEndpoint endpoint;

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly Func<IFunctionEndpoint, ExecutionContext, Task> onStart;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            readonly bool doNotFailOnErrorMessages;
            readonly bool sendsAtomicWithReceive;
            readonly Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory;
        }
    }
}