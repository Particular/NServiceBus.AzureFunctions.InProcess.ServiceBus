namespace StorageQueues.Tests
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using NServiceBus;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using NUnit.Framework;

    public class When_function_receives_a_message
    {
        [Test]
        public async Task Should_invoke_the_handler_to_process_it()
        {
            var testContext = new TestContext();

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

                return configuration;
            });

            await endpoint.Process(GenerateMessage(), new Microsoft.Azure.WebJobs.ExecutionContext());

            Assert.AreEqual(1, testContext.HandlerInvocationCount, "Handler should have been invoked once");

            CloudQueueMessage GenerateMessage()
            {
                var messageWrapper = new MessageWrapper();
                messageWrapper.Body = Encoding.UTF8.GetBytes("<HappyDayMessage/>");
                messageWrapper.Headers = new Dictionary<string, string> {{"NServiceBus.EnclosedMessageTypes", typeof(HappyDayMessage).FullName}};

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(messageWrapper));
                return message;
            }
        }

        public class TestContext
        {
            public int HandlerInvocationCount => count;

            public void HandlerInvoked() => Interlocked.Increment(ref count);

            int count;
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
                return Task.CompletedTask;
            }
        }
    }
}