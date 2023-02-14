namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class SendOnlyReceiver : IMessageReceiver
    {
        static readonly InvalidOperationException SendOnlyEndpointException = new($"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration or do not call '{nameof(IFunctionEndpoint)}.{nameof(IFunctionEndpoint.ProcessAtomic)}/{nameof(IFunctionEndpoint.ProcessNonAtomic)}'");

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw SendOnlyEndpointException;

        public Task StartReceive(CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public Task StopReceive(CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public ISubscriptionManager Subscriptions => throw SendOnlyEndpointException;
        public string Id => throw SendOnlyEndpointException;
        public string ReceiveAddress => throw SendOnlyEndpointException;
    }
}