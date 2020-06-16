namespace StorageQueues.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        public FunctionEndpointComponent(object triggerMessage, Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization = null)
        {
            this.triggerMessage = triggerMessage;
            this.configurationCustomization = configurationCustomization ?? (_ => { });
        }

        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor)
        {
            return Task.FromResult<ComponentRunner>(new FunctionRunner(triggerMessage, configurationCustomization, runDescriptor.ScenarioContext));
        }

        readonly Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization;
        object triggerMessage;

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(
                object triggerMessage,
                Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization,
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
                    var functionEndpointConfiguration = new StorageQueueTriggeredEndpointConfiguration(Name);
                    functionEndpointConfiguration.UseSerialization<NewtonsoftSerializer>();

                    var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                    endpointConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0))
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

            CloudQueueMessage GenerateMessage(object message)
            {
                var messageWrapper = new MessageWrapper();
                using (var stream = new MemoryStream())
                {
                    messageSerializer.Serialize(message, stream);
                    messageWrapper.Body = stream.ToArray();
                }

                messageWrapper.Headers = new Dictionary<string, string> {{"NServiceBus.EnclosedMessageTypes", message.GetType().FullName}};

                var cloudQueueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(messageWrapper));
                var dequeueCountProperty = typeof(CloudQueueMessage).GetProperty("DequeueCount");
                dequeueCountProperty.SetValue(cloudQueueMessage, 1);
                return cloudQueueMessage;
            }

            readonly Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            object triggerMessage;
            FunctionEndpoint endpoint;
            IMessageSerializer messageSerializer;
        }
    }
}