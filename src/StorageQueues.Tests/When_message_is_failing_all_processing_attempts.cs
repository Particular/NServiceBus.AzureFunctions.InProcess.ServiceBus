namespace StorageQueues.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using NServiceBus;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using NServiceBus.AzureFunctions.StorageQueues;
    using NServiceBus.Transport;
    using NUnit.Framework;

    public class When_message_is_failing_all_processing_attempts
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
                var configuration = new StorageQueueTriggeredEndpointConfiguration("asq");

                configuration.AdvancedConfiguration.RegisterComponents(components => components.RegisterSingleton(testContext));

                configuration.UseSerialization<XmlSerializer>();

                configuration.Transport.UnwrapMessagesWith(message => new MessageWrapper
                {
                    Id = message.Id,
                    Body = message.AsBytes,
                    Headers = new Dictionary<string, string>()
                });

                var recoverability = configuration.AdvancedConfiguration.Recoverability();
                recoverability.Immediate(settings => settings.NumberOfRetries(1));
                recoverability.Delayed(settings => settings.NumberOfRetries(0));
                recoverability.CustomPolicy(testRecoverabilityPolicy.Invoke);

                return configuration;
            });

            Task.WaitAny(endpoint.Process(GenerateMessage(), new Microsoft.Azure.WebJobs.ExecutionContext()), tcs.Task);

            Assert.AreEqual(1, testContext.HandlerInvocationCount);
            Assert.AreEqual(1, testContext.SentToErrorQueueCount);

            CloudQueueMessage GenerateMessage()
            {
                var messageWrapper = new MessageWrapper();
                messageWrapper.Body = Encoding.UTF8.GetBytes("<HappyDayMessage/>");
                messageWrapper.Headers = new Dictionary<string, string> { { "NServiceBus.EnclosedMessageTypes", typeof(HappyDayMessage).FullName } };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(messageWrapper));

                // assign test message mocked dequeue count
                var property = typeof(CloudQueueMessage).GetProperty("DequeueCount");
                property.SetValue(message, 2);

                return message;
            }

            return Task.CompletedTask;
        }

        public class TestContext
        {
            TaskCompletionSource<bool> tcs;
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