using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using NServiceBus;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: NServiceBusTriggerFunction("InProcess-HostV3", SendsAtomicWithReceive = false)]

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.UseNServiceBus();
    }
}