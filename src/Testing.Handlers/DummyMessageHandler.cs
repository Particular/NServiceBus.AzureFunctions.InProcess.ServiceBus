namespace Testing.Handlers
{
    using System.Threading.Tasks;
    using NServiceBus;

    public class DummyMessageHandler : IHandleMessages<DummyMessage>
    {
        public Task Handle(DummyMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    public class DummyMessage : IMessage
    {
    }
}