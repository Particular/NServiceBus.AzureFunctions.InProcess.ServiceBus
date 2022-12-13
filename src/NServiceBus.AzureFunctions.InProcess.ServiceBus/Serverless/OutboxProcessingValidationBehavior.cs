namespace NServiceBus.AzureFunctions.InProcess.ServiceBus.Serverless;

using System;
using System.Threading.Tasks;
using Features;
using Pipeline;
using Settings;

class OutboxProcessingValidationBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    public OutboxProcessingValidationBehavior(IReadOnlySettings settings)
    {
        outboxEnabled = settings.IsFeatureActive(typeof(Outbox));
    }

    public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        if (outboxEnabled && context.Extensions.Get<FunctionInvocationMode>().Atomic)
        {
            throw new Exception("Calling ProcessAtomic is not possible when the Outbox is enabled as it would cause a message loss in certain scenarios. Use ProcessNonAtomic for endpoints with Outbox.");
        }

        return next(context);
    }

    readonly bool outboxEnabled;
}