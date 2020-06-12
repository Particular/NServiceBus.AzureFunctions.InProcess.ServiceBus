namespace StorageQueues.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        object triggerMessage;
        readonly Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization;

        public FunctionEndpointComponent(object triggerMessage, Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization = null)
        {
            this.triggerMessage = triggerMessage;
            this.configurationCustomization = configurationCustomization ?? (_ => { });
        }

        public Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            return Task.FromResult<ComponentRunner>(new FunctionRunner(triggerMessage, configurationCustomization));
        }

        class FunctionRunner : ComponentRunner
        {
            object triggerMessage;

            readonly Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization;

            public override string Name => $"{triggerMessage.GetType().Name}Function";

            FunctionEndpoint endpoint;

            IMessageSerializer messageSerializer;

            public FunctionRunner(object triggerMessage, Action<StorageQueueTriggeredEndpointConfiguration> configurationCustomization)
            {
                this.triggerMessage = triggerMessage;
                this.configurationCustomization = configurationCustomization;

                var serializer = new NewtonsoftSerializer();
                messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());
            }

            public override Task Start(CancellationToken token)
            {
                var functionEndpoint = new TestableFunctionEndpoint(context =>
                {
                    var functionEndpointConfiguration = new StorageQueueTriggeredEndpointConfiguration(Name);
                    functionEndpointConfiguration.UseSerialization<NewtonsoftSerializer>();
                    functionEndpointConfiguration.AdvancedConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0));

                    configurationCustomization(functionEndpointConfiguration);
                    return functionEndpointConfiguration;
                });
                endpoint = functionEndpoint;
                return Task.CompletedTask;
            }

            public override Task ComponentsStarted(CancellationToken token)
            {
                var message = GenerateMessage(triggerMessage);
                var context = new ExecutionContext();
                return endpoint.Process(message, context);
            }

            CloudQueueMessage GenerateMessage(object message)
            {
                var messageWrapper = new MessageWrapper();
                using (var stream = new MemoryStream())
                {
                    messageSerializer.Serialize(message, stream);
                    messageWrapper.Body = stream.ToArray();
                }
                messageWrapper.Headers = new Dictionary<string, string> { { "NServiceBus.EnclosedMessageTypes", message.GetType().FullName } };

                var cloudQueueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(messageWrapper));
                var dequeueCountProperty = typeof(CloudQueueMessage).GetProperty("DequeueCount");
                dequeueCountProperty.SetValue(cloudQueueMessage, 1);
                return cloudQueueMessage;
            }
        }
    }
}