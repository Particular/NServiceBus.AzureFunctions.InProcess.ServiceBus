namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    class InitializationHost : IHostedService
    {
        readonly InProcessFunctionEndpoint functionEndpoint;

        public InitializationHost(InProcessFunctionEndpoint functionEndpoint) => this.functionEndpoint = functionEndpoint;

        public Task StartAsync(CancellationToken cancellationToken = default) => functionEndpoint.InitializeEndpointIfNecessary(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}