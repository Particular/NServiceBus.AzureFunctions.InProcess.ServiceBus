using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using NServiceBus;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: NServiceBusTriggerFunction(Startup.EndpointName, SendsAtomicWithReceive = true)]

public class Startup : FunctionsStartup
{
    public const string EndpointName = "InProcess-HostV4";

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.UseNServiceBus(() => new ServiceBusTriggeredEndpointConfiguration(EndpointName));
    }
}