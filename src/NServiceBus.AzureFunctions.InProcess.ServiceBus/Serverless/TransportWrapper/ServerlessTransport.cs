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
            PipelineInvoker = (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId];
            return serverlessTransportInfrastructure;
        }

        public PipelineInvoker PipelineInvoker { get; private set; }

        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => baseTransport.GetSupportedTransactionModes();

        readonly AzureServiceBusTransport baseTransport;
    }
}