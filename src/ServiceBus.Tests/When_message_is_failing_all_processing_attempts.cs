namespace ServiceBus.Tests
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using NServiceBus;
    using NServiceBus.Transport;
    using NUnit.Framework;

    public class When_message_is_failing_all_processing_attempts : BaseTest
    {
        [Test]
        public Task Should_be_moved_to_the_error_queue()
        {
            // The transport will try to talk to the service when a message is moved to the error queue. We only care about the fact that it will be sent to the error queue, so don't wait.
            var tcs = new TaskCompletionSource<bool>();
            var testContext = new TestContext(tcs);
            var testRecoverabilityPolicy = new TestRecoverabilityPolicy(testContext);

            var endpoint = new FunctionEndpoint(functionExecutionContext =>
            {
                var configuration = new ServiceBusTriggeredEndpointConfiguration("asb");

                configuration.AdvancedConfiguration.RegisterComponents(components => components.RegisterSingleton(testContext));

                var recoverability = configuration.AdvancedConfiguration.Recoverability();
                recoverability.Immediate(settings => settings.NumberOfRetries(1));
                recoverability.Delayed(settings => settings.NumberOfRetries(0));
                recoverability.CustomPolicy(testRecoverabilityPolicy.Invoke);

                return configuration;
            });

            Task.WaitAny(endpoint.Process(GenerateMessage(), CreateExecutionContext()), tcs.Task);

            Assert.AreEqual(1, testContext.HandlerInvocationCount);
            Assert.AreEqual(1, testContext.SentToErrorQueueCount);

            Message GenerateMessage()
            {
                var bytes = Encoding.UTF8.GetBytes("<HappyDayMessage/>");
                var message = new Message(bytes);
                message.UserProperties["NServiceBus.EnclosedMessageTypes"] = typeof(HappyDayMessage).FullName;

                var systemProperties = new Message.SystemPropertiesCollection();
                // sequence number is required to prevent SystemPropertiesCollection from throwing on the getters
                var fieldInfo = typeof(Message.SystemPropertiesCollection).GetField("sequenceNumber", BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfo.SetValue(systemProperties, 123);
                // set delivery count to 2 -- the message has been already attempted for a processing once
                var deliveryCountProperty = typeof(Message.SystemPropertiesCollection).GetProperty("DeliveryCount");
                deliveryCountProperty.SetValue(systemProperties, 2);
                // assign test message mocked system properties
                var property = typeof(Message).GetProperty("SystemProperties");
                property.SetValue(message, systemProperties);

                return message;
            }

            return Task.CompletedTask;
        }

        public class TestContext
        {
            public int HandlerInvocationCount => count;
            public int SentToErrorQueueCount => sentToErrorQueue;

            public void HandlerInvoked() => Interlocked.Increment(ref count);
            public void SentToErrorQueue()
            {
                Interlocked.Increment(ref sentToErrorQueue);
                tcs.TrySetResult(true);
            }

            int count;
            int sentToErrorQueue;
            TaskCompletionSource<bool> tcs;

            public TestContext(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }

        class HappyDayMessage : IMessage { }

        class HappyDayMessageHandler : IHandleMessages<HappyDayMessage>
        {
            TestContext testContext;

            public HappyDayMessageHandler(TestContext testContext)
            {
                this.testContext = testContext;
            }
            public Task Handle(HappyDayMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerInvoked();

                throw new Exception("boom");
            }
        }

        class TestRecoverabilityPolicy
        {
            readonly TestContext testContext;

            public TestRecoverabilityPolicy(TestContext testContext)
            {
                this.testContext = testContext;
            }

            public RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
            {
                var action = DefaultRecoverabilityPolicy.Invoke(config, errorContext);

                if (action is MoveToError)
                {
                        testContext.SentToErrorQueue();
                }

                if (action is MoveToError)
                {
                    return action;
                }

                return action;
            }
        }
    }
}