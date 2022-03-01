namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class PipelineInvoker : IMessageReceiver
    {
        public PipelineInvoker(IMessageReceiver baseTransportReceiver)
        {
            this.baseTransportReceiver = baseTransportReceiver;
        }

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext, CancellationToken cancellationToken) => onError(errorContext, cancellationToken);

        public Task PushMessage(MessageContext messageContext, CancellationToken cancellationToken) => onMessage.Invoke(messageContext, cancellationToken);

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            return baseTransportReceiver?.Initialize(limitations,
                (_, __) => Task.CompletedTask,
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                cancellationToken) ?? Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;

        public string ReceiveAddress => baseTransportReceiver.ReceiveAddress;

        readonly IMessageReceiver baseTransportReceiver;
        OnMessage onMessage;
        OnError onError;
    }
}