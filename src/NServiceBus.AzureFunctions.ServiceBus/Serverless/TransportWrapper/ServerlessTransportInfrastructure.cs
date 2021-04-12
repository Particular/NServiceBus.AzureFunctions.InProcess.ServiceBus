namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransportInfrastructure : TransportInfrastructure
    {
        public ServerlessTransportInfrastructure(
            TransportInfrastructure baseTransportInfrastructure,
            IReadOnlyDictionary<string, IMessageReceiver> receiverSettings)
        {
            this.baseTransportInfrastructure = baseTransportInfrastructure;

            Dispatcher = baseTransportInfrastructure.Dispatcher;

            Receivers = receiverSettings
                .ToDictionary<KeyValuePair<string, IMessageReceiver>, string, IMessageReceiver>(
                    r => r.Key,
                    r => new PipelineInvoker(r.Value));
        }

        public override Task Shutdown() => baseTransportInfrastructure.Shutdown();

        readonly TransportInfrastructure baseTransportInfrastructure;
    }
}