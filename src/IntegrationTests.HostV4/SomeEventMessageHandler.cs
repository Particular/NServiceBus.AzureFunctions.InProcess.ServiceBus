using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class SomeEventMessageHandler : IHandleMessages<SomeEvent>
{
    static readonly ILog Log = LogManager.GetLogger<SomeEventMessageHandler>();

    public Task Handle(SomeEvent message, IMessageHandlerContext context)
    {
        Log.Warn($"Handling {nameof(SomeEvent)} in {nameof(SomeEventMessageHandler)}");

        return Task.CompletedTask;
    }
}