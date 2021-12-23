﻿namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        public FunctionEndpointComponent()
        {
        }

        public FunctionEndpointComponent(object triggerMessage)
        {
            Messages.Add(triggerMessage);
        }

        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor)
        {
            return Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    Messages,
                    CustomizeConfiguration,
                    runDescriptor.ScenarioContext,
                    GetType()));
        }

        public IList<object> Messages { get; } = new List<object>();

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };


        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(
                IList<object> messages,
                Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext,
                Type functionComponentType)
            {
                this.messages = messages;
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            protected bool UseAtomicSendsWithReceive = true;

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name, default);
                configurationCustomization(functionEndpointConfiguration);
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

                endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                // enable installers to auto-create the input queue for tests
                // in real Azure functions the input queue is assumed to exist
                endpointConfiguration.EnableInstallers();

                var serviceCollection = new ServiceCollection();
                var startableEndpointWithExternallyManagedContainer = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = new InProcessFunctionEndpoint(startableEndpointWithExternallyManagedContainer, functionEndpointConfiguration, serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken)
            {
                var connectionString = Environment.GetEnvironmentVariable(ServiceBusTriggeredEndpointConfiguration
                        .DefaultServiceBusConnectionName);

                var client = new ServiceBusClient(connectionString);
                var serviceBusAdministrationClient = new ServiceBusAdministrationClient(connectionString);
                var functionInputQueueName = Name;

                if (await serviceBusAdministrationClient.QueueExistsAsync(functionInputQueueName, cancellationToken))
                {
                    await serviceBusAdministrationClient.DeleteQueueAsync(functionInputQueueName, cancellationToken);
                }

                await serviceBusAdministrationClient.CreateQueueAsync(functionInputQueueName, cancellationToken);

                var sender = client.CreateSender(functionInputQueueName);

                foreach (var message in messages)
                {
                    var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message))
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                    };

                    serviceBusMessage.ApplicationProperties["NServiceBus.EnclosedMessageTypes"] = message.GetType().FullName;


                    await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

                    var receiver = client.CreateReceiver(functionInputQueueName);

                    var serviceBusReceivedMessage = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
                    var messageActions = new TestableServiceBusMessageActions(receiver);
                    await endpoint.Process(serviceBusReceivedMessage, new ExecutionContext(), client, messageActions, UseAtomicSendsWithReceive, null, cancellationToken);

                    if (!UseAtomicSendsWithReceive)
                    {
                        await receiver.CompleteMessageAsync(serviceBusReceivedMessage, cancellationToken);
                    }
                }
            }

            public override Task Stop()
            {
                if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                {
                    throw new MessageFailedException(failedMessages.First(), scenarioContext);
                }

                return base.Stop();
            }

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            IList<object> messages;
            IFunctionEndpoint endpoint;
        }
    }
}