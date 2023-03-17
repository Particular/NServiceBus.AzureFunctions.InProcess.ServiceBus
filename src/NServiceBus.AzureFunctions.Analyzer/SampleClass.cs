using NServiceBus;

class Foo
{
#pragma warning disable CA1822
#pragma warning disable IDE0051 // Remove unused private members
    void Bar(ServiceBusTriggeredEndpointConfiguration obj)
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1822
    {
        obj.AdvancedConfiguration.PurgeOnStartup(true);
    }
}