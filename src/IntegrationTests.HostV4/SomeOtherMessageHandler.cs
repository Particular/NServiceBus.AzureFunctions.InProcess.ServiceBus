using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class SomeOtherMessageHandler : IHandleMessages<SomeOtherMessage>
{
    static readonly ILog Log = LogManager.GetLogger<SomeOtherMessageHandler>();

    public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
    {
        Log.Warn($"Handling {nameof(SomeOtherMessage)} in {nameof(SomeOtherMessageHandler)}");

        return Task.CompletedTask;
    }
}
