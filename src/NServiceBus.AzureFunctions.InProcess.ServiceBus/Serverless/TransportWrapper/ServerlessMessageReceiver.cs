namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessMessageReceiver : IMessageReceiver
    {
        readonly IMessageReceiver baseTransportReceiver;
        readonly PipelineInvoker pipelineInvoker;

        public ServerlessMessageReceiver(IMessageReceiver baseTransportReceiver, PipelineInvoker pipelineInvoker)
        {
            this.baseTransportReceiver = baseTransportReceiver;
            this.pipelineInvoker = pipelineInvoker;
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = new CancellationToken())
        {
            // TODO: Is there a better way to do this?
            if (Id == "Main")
            {
                pipelineInvoker.Init(onMessage, onError);
            }
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;
    }
}