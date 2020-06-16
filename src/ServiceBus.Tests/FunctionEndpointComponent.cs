namespace ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        public FunctionEndpointComponent(object triggerMessage, Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization = null)
        {
            this.triggerMessage = triggerMessage;
            this.configurationCustomization = configurationCustomization ?? (_ => { });
        }

        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor)
        {
            return Task.FromResult<ComponentRunner>(new FunctionRunner(triggerMessage, configurationCustomization, runDescriptor.ScenarioContext));
        }

        readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
        object triggerMessage;

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(
                object triggerMessage,
                Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext)
            {
                this.triggerMessage = triggerMessage;
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;

                var serializer = new NewtonsoftSerializer();
                messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());
            }

            public override string Name => $"{triggerMessage.GetType().Name}Function";

            public override Task Start(CancellationToken token)
            {
                endpoint = new TestableFunctionEndpoint(context =>
                {
                    var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name);
                    functionEndpointConfiguration.UseSerialization<NewtonsoftSerializer>();

                    var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                    endpointConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0))
                        .Delayed(d => d.NumberOfRetries(0))
                        .Failed(c => c
                            // track messages sent to the error queue to fail the test
                            .OnMessageSentToErrorQueue(failedMessage =>
                            {
                                scenarioContext.FailedMessages.AddOrUpdate(
                                    Name,
                                    new[] {failedMessage},
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
                    return functionEndpointConfiguration;
                });

                return Task.CompletedTask;
            }

            public override Task ComponentsStarted(CancellationToken token)
            {
                var message = GenerateMessage(triggerMessage);
                var context = new ExecutionContext();
                return endpoint.Process(message, context);
            }

            public override Task Stop()
            {
                if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                {
                    throw new MessageFailedException(failedMessages.First(), scenarioContext);
                }

                return base.Stop();
            }

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
                var fieldInfo = typeof(Message.SystemPropertiesCollection).GetField("sequenceNumber", BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfo.SetValue(systemProperties, 123);
                // set delivery count to 1
                var deliveryCountProperty = typeof(Message.SystemPropertiesCollection).GetProperty("DeliveryCount");
                deliveryCountProperty.SetValue(systemProperties, 1);
                // assign test message mocked system properties
                var property = typeof(Message).GetProperty("SystemProperties");
                property.SetValue(asbMessage, systemProperties);

                return asbMessage;
            }

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            object triggerMessage;
            FunctionEndpoint endpoint;
            IMessageSerializer messageSerializer;
        }
    }
}