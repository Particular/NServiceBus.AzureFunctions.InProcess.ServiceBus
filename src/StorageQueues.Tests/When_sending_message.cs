namespace StorageQueues.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_sending_message
    {
        [Test]
        public async Task Should_send_message_to_target_queue()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>()
                .WithComponent(new SendingFunction(new TriggerMessage()))
                .Done(c => c.HandlerReceivedMessage)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool HandlerReceivedMessage { get; set; }
        }

        public class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            class OutgoingMessageHandler : IHandleMessages<FollowupMessage>
            {
                Context testContext;

                public OutgoingMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FollowupMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerReceivedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class SendingFunction : FunctionEndpointComponent
        {
            public SendingFunction(object triggerMessage) : base(triggerMessage)
            {
            }

            public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    return context.Send(Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)), new FollowupMessage());
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class FollowupMessage : IMessage
        {
        }
    }
}