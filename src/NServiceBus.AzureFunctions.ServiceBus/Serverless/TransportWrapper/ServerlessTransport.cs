namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransport<TBaseTransport> : TransportDefinition
        where TBaseTransport : TransportDefinition
    {
        const string MainReceiverId = "Main"; // the fixed ID of the main receiver, assigned by core.

        readonly TBaseTransport baseTransport;

        public ServerlessTransport(TBaseTransport baseTransport)
            : base(TransportTransactionMode.ReceiveOnly, //support ReceiveOnly so that we can use immediate retries
                baseTransport.SupportsDelayedDelivery,
                baseTransport.SupportsPublishSubscribe,
                baseTransport.SupportsTTBR)
        {
            this.baseTransport = baseTransport;
        }

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receiverSettings, string[] sendingAddresses)
        {
            var baseTransportInfrastructure = await baseTransport.Initialize(
                hostSettings,
                receiverSettings,
                sendingAddresses).ConfigureAwait(false);

            var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure, baseTransportInfrastructure.Receivers);
            MainReceiver = (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId];

            return serverlessTransportInfrastructure;
        }

        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
            baseTransport.GetSupportedTransactionModes();

        public PipelineInvoker MainReceiver { get; private set; }
    }
}