namespace NServiceBus
{
    class ServerlessAzureServiceBusTransport : AzureServiceBusTransport
    {
        internal void ChangeConnectionString(string newConnectionString) => ConnectionString = newConnectionString;
        internal string ReadConnectionString() => ConnectionString;
    }
}