namespace NServiceBus
{
    using System.Reflection;

    static class AzureServiceBusTransportExtensions
    {
        public static void ChangeConnectionString(this AzureServiceBusTransport transport, string connectionString)
        {
            var property = typeof(AzureServiceBusTransport).GetProperty("ConnectionString", BindingFlags.Instance | BindingFlags.NonPublic);
            property.SetValue(transport, connectionString);
        }
    }
}
