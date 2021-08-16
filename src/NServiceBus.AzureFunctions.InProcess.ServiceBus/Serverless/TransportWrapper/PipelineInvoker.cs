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

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext) => onError(errorContext);

        public Task PushMessage(MessageContext messageContext) => onMessage.Invoke(messageContext);

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this.onMessage = onMessage;
            this.onError = onError;
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;

        readonly IMessageReceiver baseTransportReceiver;
        OnMessage onMessage;
        OnError onError;
    }
}