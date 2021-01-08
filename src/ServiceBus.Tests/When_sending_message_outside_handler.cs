namespace ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_sending_message_outside_handler
    {
        [Test]
        public async Task Should_send_message_to_target_queue()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(b => b.When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    await session.Send(new TriggerMessage(), sendOptions);
                }))
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

            class TriggerMessageHandler : IHandleMessages<TriggerMessage>
            {
                Context testContext;

                public TriggerMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerReceivedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }
    }
}