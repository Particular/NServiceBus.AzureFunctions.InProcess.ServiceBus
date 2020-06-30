namespace NServiceBus.AzureFunctions
{
    using ServiceBus;
    using Settings;
    using Transport;

    class ServerlessTransport<TBaseTransport> : TransportDefinition
        where TBaseTransport : TransportDefinition, new()
    {
        public ServerlessTransport()
        {
            baseTransport = new TBaseTransport();
        }

        public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;

        public override bool RequiresConnectionString => baseTransport.RequiresConnectionString;

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            var baseTransportInfrastructure = baseTransport.Initialize(settings, connectionString);

            return new ServerlessTransportInfrastructure(baseTransportInfrastructure, settings);
        }

        readonly TBaseTransport baseTransport;
    }
}