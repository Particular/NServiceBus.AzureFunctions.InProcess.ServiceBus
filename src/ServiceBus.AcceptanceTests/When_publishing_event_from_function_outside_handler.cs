namespace ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_publishing_event_from_function_outside_handler
    {
        [Test]
        public async Task Should_publish_to_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<OutsideSubscriberEndpoint>(b =>
                    b.When(async session => await session.Publish(new OutsideTestEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class OutsideSubscriberEndpoint : EndpointConfigurationBuilder
        {
            public OutsideSubscriberEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            public class EventHandler : IHandleMessages<OutsideTestEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(OutsideTestEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class OutsideTestEvent : IEvent
        {
        }
    }
}