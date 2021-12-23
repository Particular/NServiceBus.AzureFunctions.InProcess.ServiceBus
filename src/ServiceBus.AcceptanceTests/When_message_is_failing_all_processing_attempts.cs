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
        [Test]
        public void Should_be_moved_to_the_error_queue()
        {
            Context testContext = null;
            var exception = Assert.ThrowsAsync<MessageFailedException>(() =>
            {
                return Scenario.Define<Context>(c => testContext = c)
                    .WithComponent(new MoveToErrorQueueFunction(new TriggerMessage()))
                    .Done(c => c.EndpointsStarted)
                    .Run();
            });

            Assert.AreEqual(1, testContext.HandlerInvocations, "the handler should only be invoked once");
            Assert.IsInstanceOf<SimulatedException>(exception.InnerException, "it should be the exception from the handler");
            Assert.AreEqual(1, testContext.FailedMessages.Single().Value.Count, "there should be only one failed message");
        }

        class Context : ScenarioContext
        {
            public int HandlerInvocations { get; set; }
        }

        class MoveToErrorQueueFunction : FunctionEndpointComponent
        {
            public MoveToErrorQueueFunction(object triggerMessage) : base(triggerMessage)
            {
            }

            public class FailingHandler : IHandleMessages<TriggerMessage>
            {
                Context testContext;

                public FailingHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvocations++;
                    throw new SimulatedException();
                }
            }
        }

        public class TriggerMessage : IMessage
        {
        }
    }
}