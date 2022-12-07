using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using NServiceBus;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: NServiceBusTriggerFunction("InProcess-HostV4", SendsAtomicWithReceive = false)]

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.UseNServiceBus(ec =>
        {
            ec.AdvancedConfiguration.UsePersistence<NonDurablePersistence>();

            ec.AdvancedConfiguration.EnableOutbox();
            //ec.Transport.TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
        });
    }
}