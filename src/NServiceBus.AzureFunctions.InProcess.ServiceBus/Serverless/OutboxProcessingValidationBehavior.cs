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
            throw new Exception("Atomic sends with receive is not supported when the Outbox is enabled as it would risk message loss. Set `SendsAtomicWithReceive` to `false` on the `NServiceBusTriggerFunction` attribute or make sure to call `ProcessNonAtomic` instead of `ProcessAtomic` if using a custom trigger.");
        }

        return next(context);
    }

    readonly bool outboxEnabled;
}