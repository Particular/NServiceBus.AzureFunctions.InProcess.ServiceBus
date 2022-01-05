namespace ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_publishing_event_from_function
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public async Task Should_publish_to_subscribers(TransportTransactionMode transactionMode)
        {
            var context = await Scenario.Define<Context>()
<<<<<<< HEAD:src/ServiceBus.Tests/When_publishing_event_from_function.cs
                .WithEndpoint<InsideSubscriberEndpoint>()
                .WithComponent(new PublishingFunction())
=======
                .WithEndpoint<InsideEndpoint>()
                .WithComponent(new PublishingFunction(transactionMode))
>>>>>>> 6fca7ee (Update to Microsoft.Azure.WebJobs.Extensions.ServiceBus 5.2.0 (#393)):src/ServiceBus.AcceptanceTests/When_publishing_event_from_function.cs
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class InsideSubscriberEndpoint : EndpointConfigurationBuilder
        {
            public InsideSubscriberEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            public class EventHandler : IHandleMessages<InsideTestEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(InsideTestEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class PublishingFunction : FunctionEndpointComponent
        {
            public PublishingFunction(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                Messages.Add(new TriggerMessage());
            }

            public class PublishingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    return context.Publish(new InsideTestEvent());
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class InsideTestEvent : IEvent
        {
        }
    }
}