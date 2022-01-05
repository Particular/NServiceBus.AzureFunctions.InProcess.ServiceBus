namespace ServiceBus.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_function_receives_a_message
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public async Task Should_invoke_the_handler_to_process_it(TransportTransactionMode transactionMode)
        {
            var context = await Scenario.Define<Context>()
                .WithComponent(new FunctionHandler(transactionMode))
                .Done(c => c.HandlerInvocationCount > 0)
                .Run();

            Assert.AreEqual(1, context.HandlerInvocationCount);
        }

        public class Context : ScenarioContext
        {
            public int HandlerInvocationCount => count;

            public void HandlerInvoked() => Interlocked.Increment(ref count);

            int count;
        }

        class FunctionHandler : FunctionEndpointComponent
        {
            public FunctionHandler(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                Messages.Add(new HappyDayMessage());
            }

            public class HappyDayMessageHandler : IHandleMessages<HappyDayMessage>
            {
                Context testContext;

                public HappyDayMessageHandler(Context testContext)
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

        class HappyDayMessage : IMessage
        {
        }
    }
}