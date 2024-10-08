﻿namespace ServiceBus.Tests
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
                .WithEndpoint<InsideEndpoint>()
                .WithComponent(new PublishingFunction(transactionMode))
                .Done(c => c.EventReceived)
                .Run();

            Assert.That(context.EventReceived, Is.True);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class InsideEndpoint : EndpointConfigurationBuilder
        {
            public InsideEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            public class EventHandler : IHandleMessages<InsideEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(InsideEvent message, IMessageHandlerContext context)
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
                    return context.Publish(new InsideEvent());
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class InsideEvent : IEvent
        {
        }
    }
}