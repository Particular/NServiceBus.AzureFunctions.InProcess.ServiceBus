﻿namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
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

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext, CancellationToken cancellationToken = default) => onError(errorContext, cancellationToken);

        public Task PushMessage(MessageContext messageContext, CancellationToken cancellationToken = default) => onMessage.Invoke(messageContext, cancellationToken);

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = default)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;

        readonly IMessageReceiver baseTransportReceiver;
        OnMessage onMessage;
        OnError onError;
    }
}