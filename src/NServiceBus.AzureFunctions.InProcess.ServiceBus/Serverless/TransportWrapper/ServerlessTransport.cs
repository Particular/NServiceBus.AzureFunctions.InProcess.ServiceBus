namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransport : TransportDefinition
    {
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
            CancellationToken cancellationToken = new CancellationToken())
        {
            var baseTransportInfrastructure = await baseTransport.Initialize(
                    hostSettings,
                    receivers,
                    sendingAddresses,
                    cancellationToken)
                .ConfigureAwait(false);

            return new ServerlessTransportInfrastructure(baseTransportInfrastructure, PipelineInvoker);
        }

        public PipelineInvoker PipelineInvoker { get; } = new PipelineInvoker();

        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => baseTransport.GetSupportedTransactionModes();

        readonly AzureServiceBusTransport baseTransport;
    }
}