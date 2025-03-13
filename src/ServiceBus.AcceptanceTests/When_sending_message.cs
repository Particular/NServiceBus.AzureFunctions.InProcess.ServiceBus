namespace ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_sending_message
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public async Task Should_send_message_to_target_queue(TransportTransactionMode transactionMode)
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>()
                .WithComponent(new SendingFunction(transactionMode))
                .Done(c => c.HandlerReceivedMessage)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool HandlerReceivedMessage { get; set; }
        }

        public class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() => EndpointSetup<DefaultEndpoint>();

            class OutgoingMessageHandler(Context testContext) : IHandleMessages<FollowupMessage>
            {
                public Task Handle(FollowupMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerReceivedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class SendingFunction : FunctionEndpointComponent
        {
            public SendingFunction(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                Messages.Add(new TriggerMessage());
            }

            public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                    => context.Send(Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)), new FollowupMessage());
            }
        }

        class TriggerMessage : IMessage;

        class FollowupMessage : IMessage;
    }
}