namespace ServiceBus.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;

    public class When_message_is_failing_all_processing_attempts
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public void Should_be_moved_to_the_error_queue(TransportTransactionMode transactionMode)
        {
            Context testContext = null;
            var exception = Assert.ThrowsAsync<MessageFailedException>(() =>
            {
                return Scenario.Define<Context>(c => testContext = c)
                    .WithComponent(new MoveToErrorQueueFunction(transactionMode))
                    .Done(c => c.EndpointsStarted)
                    .Run();
            });

            Assert.Multiple(() =>
            {
                Assert.That(testContext.HandlerInvocations, Is.EqualTo(1), "the handler should only be invoked once");
                Assert.That(exception.InnerException, Is.InstanceOf<SimulatedException>(), "it should be the exception from the handler");
                Assert.That(testContext.FailedMessages.Single().Value, Has.Count.EqualTo(1), "there should be only one failed message");
            });
        }

        class Context : ScenarioContext
        {
            public int HandlerInvocations { get; set; }
        }

        class MoveToErrorQueueFunction : FunctionEndpointComponent
        {
            public MoveToErrorQueueFunction(TransportTransactionMode transactionMode) : base(transactionMode) => Messages.Add(new TriggerMessage());

            public class FailingHandler(Context testContext) : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvocations++;
                    throw new SimulatedException();
                }
            }
        }

        public class TriggerMessage : IMessage;
    }
}