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
    using NServiceBus.Transport.AzureServiceBus;
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
                    HostConfigurationCustomization,
                    OnStartCore,
                    runDescriptor.ScenarioContext,
                    PublisherMetadata,
                    GetType(),
                    DoNotFailOnErrorMessages,
                    TypesScopedByTestClassAssemblyScanningEnabled,
                    sendsAtomicWithReceive,
                    ServiceBusMessageActionsFactory));

        protected IList<object> Messages { get; } = [];

        protected bool DoNotFailOnErrorMessages { get; init; }

        protected bool TypesScopedByTestClassAssemblyScanningEnabled { get; init; } = true;

        protected Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> ServiceBusMessageActionsFactory { get; init; } = (r, _) => new TestableServiceBusMessageActions(r);

        protected Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; init; } = _ => { };

        protected Action<IConfigurationBuilder> HostConfigurationCustomization { private get; init; } = _ => { };

        protected PublisherMetadata PublisherMetadata { get; } = new PublisherMetadata();

        protected virtual Task OnStart(IFunctionEndpoint functionEndpoint, ExecutionContext executionContext) => Task.CompletedTask;

        Task OnStartCore(IFunctionEndpoint functionEndpoint, ExecutionContext executionContext) => OnStart(functionEndpoint, executionContext);

        readonly bool sendsAtomicWithReceive;

        class FunctionRunner(IList<object> messages,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
            Action<IConfigurationBuilder> hostConfigurationCustomization,
            Func<IFunctionEndpoint, ExecutionContext, Task> onStart,
            ScenarioContext scenarioContext,
            PublisherMetadata publisherMetadata,
            Type functionComponentType,
            bool doNotFailOnErrorMessages,
            bool typesScopedByTestClassAssemblyScanningEnabled,
            bool sendsAtomicWithReceive,
            Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory) : ComponentRunner
        {
            public override string Name { get; } = Conventions.EndpointNamingConvention(functionComponentType);

            public override async Task Start(CancellationToken cancellationToken = default)
            {
                var hostBuilder = new FunctionHostBuilder(hostConfigurationCustomization);
                hostBuilder.UseNServiceBus(Name, triggerConfiguration =>
                {
                    var endpointConfiguration = triggerConfiguration.AdvancedConfiguration;

                    if (typesScopedByTestClassAssemblyScanningEnabled)
                    {
                        endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());
                    }

                    if (triggerConfiguration.Transport.Topology is TopicPerEventTopology topology)
                    {
                        topology.OverrideSubscriptionNameFor(Name, Name.Shorten());

                        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
                        {
                            topology.PublishTo(eventType, eventType.ToTopicName());
                            topology.SubscribeTo(eventType, eventType.ToTopicName());
                        }
                    }

                    endpointConfiguration.EnforcePublisherMetadataRegistration(Name, publisherMetadata);

                    endpointConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0))
                        .Delayed(d => d.NumberOfRetries(0))
                        .Failed(c => c
                            // track messages sent to the error queue to fail the test
                            .OnMessageSentToErrorQueue((failedMessage, ct) =>
                            {
                                _ = scenarioContext.FailedMessages.AddOrUpdate(
                                    Name,
                                    [failedMessage],
                                    (_, fm) =>
                                    {
                                        var failedMessages = fm.ToList();
                                        failedMessages.Add(failedMessage);
                                        return failedMessages;
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
            sealed class FunctionHostBuilder(Action<IConfigurationBuilder> configurationCustomization) : IFunctionsHostBuilder, IFunctionsHostBuilderExt
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
                        configurationCustomization(configurationBuilder);
                        var configurationRoot = configurationBuilder.Build();
                        context = new HostBuilderContext(new WebJobsBuilderContext { Configuration = configurationRoot, ApplicationRootPath = AppDomain.CurrentDomain.BaseDirectory });
                        return context;
                    }
                }

                public IHost Build()
                {
                    _ = hostBuilder.ConfigureHostConfiguration(configuration =>
                    {
                        configuration.AddConfiguration(Context.Configuration);
                    });
                    // Forwarding all the service registrations to the host builder
                    _ = hostBuilder.ConfigureServices(services =>
                    {
                        _ = services.AddHostedService<InitializationHost>();
                        foreach (var service in Services)
                        {
                            services.Add(service);
                        }
                        Services.Clear();
                    });
                    return hostBuilder.Build();
                }

                sealed class HostBuilderContext(WebJobsBuilderContext webJobsBuilderContext)
                    : FunctionsHostBuilderContext(webJobsBuilderContext);
            }

            IHost host;
            IFunctionEndpoint endpoint;
        }
    }
}