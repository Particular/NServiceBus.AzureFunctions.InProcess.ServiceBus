namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
#pragma warning disable IDE0005 // Using directive is unnecessary.
    using Microsoft.Azure.ServiceBus.Core;
#pragma warning restore IDE0005 // Using directive is unnecessary.
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
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
                Name = functionComponentType.FullName;

                var serializer = new NewtonsoftSerializer();
                messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name);
                var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

                endpointConfiguration.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(d => d.NumberOfRetries(0))
                    .Failed(c => c
                        // track messages sent to the error queue to fail the test
                        .OnMessageSentToErrorQueue(failedMessage =>
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

                endpointConfiguration.RegisterComponents(c => c.RegisterSingleton(scenarioContext.GetType(), scenarioContext));

                configurationCustomization(functionEndpointConfiguration);

                var serviceCollection = new ServiceCollection();
                var startableEndpointWithExternallyManagedContainer = EndpointWithExternallyManagedServiceProvider.Create(functionEndpointConfiguration.EndpointConfiguration, serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = new FunctionEndpoint(startableEndpointWithExternallyManagedContainer, functionEndpointConfiguration, serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken token)
            {
                foreach (var message in messages)
                {
#if !TRANSACTIONAL
                    await ProcessNonTransactional(message);
#else
                    await ProcessTransactional(message);
#endif
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

#pragma warning disable IDE0051 // Remove unused private members
            async Task ProcessTransactional(object message)

            {
                var transportMessage = GenerateMessage(message);
                var context = new ExecutionContext();
                var messageReceiver = new FakeMessageReceiver();
                try
                {
                    await endpoint.ProcessTransactional(transportMessage, context, messageReceiver);

                    if (!messageReceiver.CompletedLockTokens.Contains(transportMessage.SystemProperties.LockToken))
                    {
                        throw new Exception($"Message {transportMessage.MessageId} succeeded transactional processing but was not completed on the message receiver!");
                    }
                }
                catch (Exception e)
                {
                    if (!messageReceiver.AbandonedLockTokens.Contains(transportMessage.SystemProperties.LockToken))
                    {
                        throw new Exception($"Message {transportMessage.MessageId} failed transactional processing but was not abandoned on the message receiver!", e);
                    }
                    throw;
                }
            }

            Task ProcessNonTransactional(object message)
            {
                var transportMessage = GenerateMessage(message);
                var context = new ExecutionContext();
                return endpoint.Process(transportMessage, context);
            }

#pragma warning restore IDE0051 // Remove unused private members

            Message GenerateMessage(object message)
            {
                Message asbMessage;
                using (var stream = new MemoryStream())
                {
                    messageSerializer.Serialize(message, stream);
                    asbMessage = new Message(stream.ToArray());
                }

                asbMessage.UserProperties["NServiceBus.EnclosedMessageTypes"] = message.GetType().FullName;

                var systemProperties = new Message.SystemPropertiesCollection();

                // sequence number is required to prevent SystemPropertiesCollection from throwing on the getters
                var sequenceNumberField = typeof(Message.SystemPropertiesCollection).GetField("sequenceNumber", BindingFlags.NonPublic | BindingFlags.Instance);
                sequenceNumberField.SetValue(systemProperties, 123);
                // set delivery count to 1
                var deliveryCountProperty = typeof(Message.SystemPropertiesCollection).GetProperty("DeliveryCount");
                deliveryCountProperty.SetValue(systemProperties, 1);
                // set a lock token
                var lockTokenProperty = typeof(Message.SystemPropertiesCollection).GetProperty("LockTokenGuid", BindingFlags.NonPublic | BindingFlags.Instance);
                lockTokenProperty.SetValue(systemProperties, Guid.NewGuid());
                // assign test message mocked system properties
                var property = typeof(Message).GetProperty("SystemProperties");
                property.SetValue(asbMessage, systemProperties);

                return asbMessage;
            }

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            IList<object> messages;
            FunctionEndpoint endpoint;
            IMessageSerializer messageSerializer;
        }
    }
}