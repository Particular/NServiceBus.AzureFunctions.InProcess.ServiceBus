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
        const string SendOnlyConfigKey = "Endpoint.SendOnly";

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

            var isSendOnly = hostSettings.CoreSettings.GetOrDefault<bool>(SendOnlyConfigKey);

            PipelineInvoker = isSendOnly
                ? new PipelineInvoker(new SendOnlyReceiver()) // send-only endpoint
                : (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId];

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
}