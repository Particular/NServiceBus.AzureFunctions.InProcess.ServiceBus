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
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Logging;
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

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { set; private get; } = (_ => { });


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
                this.Name = functionComponentType.FullName;

                var serializer = new NewtonsoftSerializer();
                messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                endpoint = new TestableFunctionEndpoint(context =>
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

            public override async Task ComponentsStarted(CancellationToken token)
            {
                foreach (var message in messages)
                {
                    var transportMessage = GenerateMessage(message);
                    var context = new ExecutionContext();
                    await endpoint.Process(transportMessage, context, new FakeLogger(), new FakeMessageReceiver());
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
            readonly Type functionComponentType;
            IList<object> messages;
            FunctionEndpoint endpoint;
            IMessageSerializer messageSerializer;
        }

        class FakeLogger : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {

            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }

        class FakeMessageReceiver : IMessageReceiver
        {
            public Task CloseAsync() => Task.CompletedTask;

            public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
            {
            }

            public void UnregisterPlugin(string serviceBusPluginName)
            {
            }

            public string ClientId { get; }
            public bool IsClosedOrClosing { get; }
            public string Path { get; }
            public TimeSpan OperationTimeout { get; set; }
            public ServiceBusConnection ServiceBusConnection { get; }
            public bool OwnsConnection { get; }
            public IList<ServiceBusPlugin> RegisteredPlugins { get; }

            public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, Func<ExceptionReceivedEventArgs, Task> exceptionReceivedHandler)
            {
            }

            public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, MessageHandlerOptions messageHandlerOptions)
            {
            }

            public Task CompleteAsync(string lockToken) => Task.CompletedTask;

            public Task AbandonAsync(string lockToken, IDictionary<string, object> propertiesToModify = null) => Task.CompletedTask;

            public Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null) => Task.CompletedTask;

            public Task DeadLetterAsync(string lockToken, string deadLetterReason, string deadLetterErrorDescription = null) => Task.CompletedTask;

            public int PrefetchCount { get; set; }
            public ReceiveMode ReceiveMode { get; }
            public Task<Message> ReceiveAsync() => throw new NotImplementedException();

            public Task<Message> ReceiveAsync(TimeSpan operationTimeout) => throw new NotImplementedException();

            public Task<IList<Message>> ReceiveAsync(int maxMessageCount) => throw new NotImplementedException();

            public Task<IList<Message>> ReceiveAsync(int maxMessageCount, TimeSpan operationTimeout) => throw new NotImplementedException();

            public Task<Message> ReceiveDeferredMessageAsync(long sequenceNumber) => throw new NotImplementedException();

            public Task<IList<Message>> ReceiveDeferredMessageAsync(IEnumerable<long> sequenceNumbers) => throw new NotImplementedException();

            public Task CompleteAsync(IEnumerable<string> lockTokens) => throw new NotImplementedException();

            public Task DeferAsync(string lockToken, IDictionary<string, object> propertiesToModify = null) => throw new NotImplementedException();

            public Task RenewLockAsync(Message message) => throw new NotImplementedException();

            public Task<DateTime> RenewLockAsync(string lockToken) => throw new NotImplementedException();

            public Task<Message> PeekAsync() => throw new NotImplementedException();

            public Task<IList<Message>> PeekAsync(int maxMessageCount) => throw new NotImplementedException();

            public Task<Message> PeekBySequenceNumberAsync(long fromSequenceNumber) => throw new NotImplementedException();

            public Task<IList<Message>> PeekBySequenceNumberAsync(long fromSequenceNumber, int messageCount) => throw new NotImplementedException();

            public long LastPeekedSequenceNumber { get; }
        }
    }
}