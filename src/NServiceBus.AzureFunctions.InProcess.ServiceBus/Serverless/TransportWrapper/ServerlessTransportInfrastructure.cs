namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransportInfrastructure : TransportInfrastructure
    {
        public ServerlessTransportInfrastructure(TransportInfrastructure baseTransportInfrastructure, PipelineInvoker pipelineInvoker)
        {
            Dispatcher = baseTransportInfrastructure.Dispatcher;
            Receivers = baseTransportInfrastructure.Receivers.ToDictionary(
                r => r.Key,
                r => (IMessageReceiver)new ServerlessMessageReceiver(r.Value, pipelineInvoker)
                );
        }

        public override Task Shutdown(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;
    }
}