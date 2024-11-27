namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Azure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;

    class ServerlessTransport : TransportDefinition
    {
        // HINT: This constant is defined in NServiceBus but is not exposed
        const string MainReceiverId = "Main";
        const string SendOnlyConfigKey = "Endpoint.SendOnly";

        public IMessageProcessor MessageProcessor { get; private set; }

        public IServiceProvider ServiceProvider { get; set; }

        public ServerlessTransport(TransportExtensions<AzureServiceBusTransport> transportExtensions, string connectionString, string connectionName) : base(
            transportExtensions.Transport.TransportTransactionMode,
            transportExtensions.Transport.SupportsDelayedDelivery,
            transportExtensions.Transport.SupportsPublishSubscribe,
            transportExtensions.Transport.SupportsTTBR)
        {
            this.transportExtensions = transportExtensions;
            this.connectionString = connectionString;
            this.connectionName = connectionName;
        }

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            var configuredTransport = ConfigureTransportConnection(connectionString, connectionName, ServiceProvider.GetRequiredService<IConfiguration>(), transportExtensions,
                ServiceProvider.GetRequiredService<AzureComponentFactory>());

            var baseTransportInfrastructure = await configuredTransport.Initialize(
                    hostSettings,
                    receivers,
                    sendingAddresses,
                    cancellationToken)
                .ConfigureAwait(false);

            var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure);

            var isSendOnly = hostSettings.CoreSettings.GetOrDefault<bool>(SendOnlyConfigKey);

            MessageProcessor = isSendOnly
                ? new SendOnlyMessageProcessor() // send-only endpoint
                : (IMessageProcessor)serverlessTransportInfrastructure.Receivers[MainReceiverId];

            return serverlessTransportInfrastructure;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
            supportedTransactionModes;

        // We are deliberately using the old way of configuring a transport here because it allows us configuring
        // the uninitialized transport with a connection string or a fully qualified name and a token provider.
        // Once we deprecate the old way we can for example add make the internal ConnectionString, FQDN or
        // TokenProvider properties visible to functions or the code base has already moved into a different direction.
        static AzureServiceBusTransport ConfigureTransportConnection(string connectionString, string connectionName, IConfiguration configuration,
            TransportExtensions<AzureServiceBusTransport> transportExtensions, AzureComponentFactory azureComponentFactory)
        {
            if (connectionString != null)
            {
                _ = transportExtensions.ConnectionString(connectionString);
            }
            else
            {
                var serviceBusConnectionName = string.IsNullOrWhiteSpace(connectionName) ? DefaultServiceBusConnectionName : connectionName;
                IConfigurationSection connectionSection = configuration.GetSection(serviceBusConnectionName);
                if (!connectionSection.Exists())
                {
                    throw new Exception($"Azure Service Bus connection string/section has not been configured. Specify a connection string through IConfiguration, an environment variable named {serviceBusConnectionName} or passing it to `UseNServiceBus(ENDPOINTNAME,CONNECTIONSTRING)`");
                }

                if (!string.IsNullOrWhiteSpace(connectionSection.Value))
                {
                    _ = transportExtensions.ConnectionString(connectionSection.Value);
                }
                else
                {
                    string fullyQualifiedNamespace = connectionSection["fullyQualifiedNamespace"];
                    if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
                    {
                        throw new Exception("Connection should have an 'fullyQualifiedNamespace' property or be a string representing a connection string.");
                    }

                    var credential = azureComponentFactory.CreateTokenCredential(connectionSection);
                    _ = transportExtensions.CustomTokenCredential(fullyQualifiedNamespace, credential);
                }
            }

            return transportExtensions.Transport;
        }

        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";

        readonly TransportTransactionMode[] supportedTransactionModes =
        {
            TransportTransactionMode.ReceiveOnly,
            TransportTransactionMode.SendsAtomicWithReceive
        };
        readonly TransportExtensions<AzureServiceBusTransport> transportExtensions;
        readonly string connectionString;
        readonly string connectionName;
    }
}