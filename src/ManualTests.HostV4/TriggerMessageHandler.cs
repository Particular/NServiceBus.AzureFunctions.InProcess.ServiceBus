using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
{
    static readonly ILog Log = LogManager.GetLogger<TriggerMessageHandler>();

    public async Task Handle(TriggerMessage message, IMessageHandlerContext context)
    {
        Log.Warn($"Handling {nameof(TriggerMessage)} in {nameof(TriggerMessageHandler)}");

        await context.SendLocal(new SomeOtherMessage()).ConfigureAwait(false);
        await context.Publish(new SomeEvent()).ConfigureAwait(false);
    }
}
