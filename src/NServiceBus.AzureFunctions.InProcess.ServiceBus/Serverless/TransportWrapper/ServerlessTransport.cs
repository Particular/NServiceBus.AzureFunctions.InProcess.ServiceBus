namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransport : TransportDefinition
    {
        // HINT: This constant is defined in NServiceBus but is not exposed
        const string MainReceiverId = "Main";

        public ServerlessTransport(AzureServiceBusTransport baseTransport) : base(
            baseTransport.TransportTransactionMode,
            baseTransport.SupportsDelayedDelivery,
            baseTransport.SupportsPublishSubscribe,
            baseTransport.SupportsTTBR)
        {
            this.baseTransport = baseTransport;
        }

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            var baseTransportInfrastructure = await baseTransport.Initialize(
                    hostSettings,
                    receivers,
                    sendingAddresses,
                    cancellationToken)
                .ConfigureAwait(false);

            var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure);

            PipelineInvoker = receivers.Length > 0
                ? (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId]
                : new PipelineInvoker(new NullReceiver()); // send-only endpoint

            return serverlessTransportInfrastructure;
        }

        public PipelineInvoker PipelineInvoker { get; private set; }

#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
            supportedTransactionModes;

        readonly AzureServiceBusTransport baseTransport;
        readonly TransportTransactionMode[] supportedTransactionModes =
        {
            TransportTransactionMode.ReceiveOnly,
            TransportTransactionMode.SendsAtomicWithReceive
        };
    }

    class NullReceiver : IMessageReceiver
    {
        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new System.NotImplementedException();

        public Task StartReceive(CancellationToken cancellationToken = new CancellationToken()) => throw new System.NotImplementedException();

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = new CancellationToken()) => throw new System.NotImplementedException();

        public Task StopReceive(CancellationToken cancellationToken = new CancellationToken()) => throw new System.NotImplementedException();

        public ISubscriptionManager Subscriptions { get; }
        public string Id { get; }
        public string ReceiveAddress { get; }
    }
}