namespace Testing.Handlers
{
    using System.Threading.Tasks;
    using NServiceBus;

    /**
     * These types are used to test the assembly scanning logic which is required to load additional handler assemblies
     * which are optimized away when the calling code doesn't explicitly reference a type from the assembly.
     * To update the assembly used by the tests, rebuild this project and replace the dll which has been copied to the
     * test projects manually.
     * Referencing this project from the test project doesn't work as Core's assembly scanning mechanism will find and load it.
     */
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