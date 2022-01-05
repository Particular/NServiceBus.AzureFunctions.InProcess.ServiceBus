using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using NServiceBus;

[assembly: FunctionsStartup(typeof(Startup))]
<<<<<<< HEAD
[assembly: NServiceBusTriggerFunction(Startup.EndpointName)]
=======
[assembly: NServiceBusTriggerFunction("InProcess-HostV3", SendsAtomicWithReceive = false)]
>>>>>>> 6fca7ee (Update to Microsoft.Azure.WebJobs.Extensions.ServiceBus 5.2.0 (#393))

public class Startup : FunctionsStartup
{
    public const string EndpointName = "InProcess-HostV3";

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.UseNServiceBus(() => new ServiceBusTriggeredEndpointConfiguration(EndpointName));
    }
}