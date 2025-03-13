namespace NServiceBus.AzureFunctions.InProcess.ServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

class InitializationHost(InProcessFunctionEndpoint functionEndpoint) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken = default) => functionEndpoint.InitializeEndpointIfNecessary(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}