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
                .WithEndpoint<SubscriberEndpoint>(b =>
                    b.When(async session => await session.Publish(new TestEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class SubscriberEndpoint : EndpointConfigurationBuilder
        {
            public SubscriberEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            public class EventHandler : IHandleMessages<TestEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TestEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class TestEvent : IEvent
        {
        }
    }
}