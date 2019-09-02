namespace ServiceBus.Tests
{
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging.Abstractions;
    using NServiceBus;
    using NServiceBus.AzureFunctions.ServiceBus;
    using NUnit.Framework;

    public class When_function_receives_a_message
    {
        [Test]
        public async Task Should_invoke_the_handler_to_process_it()
        {
            var testContext = new TestContext();

            var endpoint = new FunctionEndpoint(functionExecutionContext =>
            {
                var configuration = new ServiceBusTriggeredEndpointConfiguration("asb", NullLogger.Instance);

                configuration.AdvancedConfiguration.RegisterComponents(components => components.RegisterSingleton(testContext));

                return configuration;
            });

            await endpoint.Process(GenerateMessage(), new Microsoft.Azure.WebJobs.ExecutionContext());

            Assert.AreEqual(1, testContext.HandlerInvocationCount, "Handler should have been invoked once");

            Message GenerateMessage()
            {
                var bytes = Encoding.UTF8.GetBytes("<HappyDayMessage/>");
                var message = new Message(bytes);
                message.UserProperties["NServiceBus.EnclosedMessageTypes"] = typeof(HappyDayMessage).FullName;

                return message;
            }
        }
    }

    public class TestContext
    {
        public int HandlerInvocationCount => count;

        public void HandlerInvoked() => Interlocked.Increment(ref count);

        int count;
    }

    class HappyDayMessage : IMessage {}

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