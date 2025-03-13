namespace NServiceBus.AzureFunctions.InProcess.ServiceBus;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class ServerlessTransportInfrastructure : TransportInfrastructure
{
    readonly TransportInfrastructure baseTransportInfrastructure;

    public ServerlessTransportInfrastructure(TransportInfrastructure baseTransportInfrastructure)
    {
        this.baseTransportInfrastructure = baseTransportInfrastructure;
        Dispatcher = baseTransportInfrastructure.Dispatcher;
        Receivers = baseTransportInfrastructure.Receivers.ToDictionary(
            r => r.Key,
            r => (IMessageReceiver)new PipelineInvokingMessageProcessor(r.Value)
        );
    }

    public override Task Shutdown(CancellationToken cancellationToken = default)
        => baseTransportInfrastructure.Shutdown(cancellationToken);
    public override string ToTransportAddress(QueueAddress address)
        => baseTransportInfrastructure.ToTransportAddress(address);
}