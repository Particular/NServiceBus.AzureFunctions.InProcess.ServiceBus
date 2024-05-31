namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.Extensions.DependencyInjection;
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
        public FunctionEndpointComponent(TransportTransactionMode transactionMode)
        {
            if (transactionMode == TransportTransactionMode.SendsAtomicWithReceive)
            {
                sendsAtomicWithReceive = true;
            }
            else if (transactionMode == TransportTransactionMode.ReceiveOnly)
            {
                sendsAtomicWithReceive = false;
            }
            else
            {
                throw new Exception("Unsupported transaction mode " + transactionMode);
            }
        }

        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor)
        {
            return Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    Messages,
                    CustomizeConfiguration,
                    runDescriptor.ScenarioContext,
                    GetType(),
                    DoNotFailOnErrorMessages,
                    sendsAtomicWithReceive,
                    ServiceBusMessageActionsFactory));
        }

        public IList<object> Messages { get; } = new List<object>();

        public bool DoNotFailOnErrorMessages { get; set; }

        public Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> ServiceBusMessageActionsFactory { get; set; } = (r, _) => new TestableServiceBusMessageActions(r);

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

        readonly bool sendsAtomicWithReceive;

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(IList<object> messages,
                Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext,
                Type functionComponentType,
                bool doNotFailOnErrorMessages,
                bool sendsAtomicWithReceive,
                Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory)
            {
                this.messages = messages;
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                this.doNotFailOnErrorMessages = doNotFailOnErrorMessages;
                this.sendsAtomicWithReceive = sendsAtomicWithReceive;
                this.serviceBusMessageActionsFactory = serviceBusMessageActionsFactory;

                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name, default, null);
                var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

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

                configurationCustomization(functionEndpointConfiguration);
                var serverless = functionEndpointConfiguration.MakeServerless();

                endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                endpointConfiguration.RegisterComponents(c => c.AddSingleton<IMutateOutgoingTransportMessages>(b => new TestIndependenceMutator(scenarioContext)));

                var serviceCollection = new ServiceCollection();
                var startableEndpointWithExternallyManagedContainer = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = new InProcessFunctionEndpoint(startableEndpointWithExternallyManagedContainer, serverless, serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken)
            {
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
                        MessageId = messageId
                    };

                    serviceBusMessage.ApplicationProperties["NServiceBus.EnclosedMessageTypes"] = message.GetType().FullName;

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

            public override Task Stop()
            {
                if (!doNotFailOnErrorMessages)
                {
                    if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                    {
                        throw new MessageFailedException(failedMessages.First(), scenarioContext);
                    }
                }

                return base.Stop();
            }

            IList<object> messages;
            IFunctionEndpoint endpoint;

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            readonly bool doNotFailOnErrorMessages;
            readonly bool sendsAtomicWithReceive;
            readonly Func<ServiceBusReceiver, ScenarioContext, ServiceBusMessageActions> serviceBusMessageActionsFactory;
        }
    }
}