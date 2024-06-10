using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using NServiceBus;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: NServiceBusTriggerFunction("InProcess-HostV4", SendsAtomicWithReceive = true)]

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.UseNServiceBus(c => c.AdvancedConfiguration.EnableInstallers());
    }
}