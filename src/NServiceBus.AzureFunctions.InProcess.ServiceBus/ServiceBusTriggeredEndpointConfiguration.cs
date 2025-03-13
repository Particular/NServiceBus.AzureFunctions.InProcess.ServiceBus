namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using AzureFunctions.InProcess.ServiceBus.Serverless;
    using Configuration.AdvancedExtensibility;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Serialization;
    using Settings;
    using Transport.AzureServiceBus;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint.
    /// </summary>
    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        static ServiceBusTriggeredEndpointConfiguration() => LogManager.UseFactory(FunctionsLoggerFactory.Instance);

        // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
        Func<string, CancellationToken, Task> customDiagnosticsWriter = (_, __) => Task.CompletedTask;

        /// <summary>
        /// The Azure Service Bus Transport configuration.
        /// </summary>
        public AzureServiceBusTransport Transport { get; }

        /// <summary>
        /// The routing configuration.
        /// </summary>
        public RoutingSettings<AzureServiceBusTransport> Routing { get; }

        /// <summary>
        /// Gives access to the underlying endpoint configuration for advanced configuration options.
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration, string connectionString = default, string connectionName = default)
        {
            this.connectionString = connectionString;
            this.connectionName = connectionName;
            var endpointConfiguration = new EndpointConfiguration(endpointName);

            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            recoverability.CustomPolicy(recoverabilityPolicy.Invoke);

            endpointConfiguration.Pipeline.Register(b => new OutboxProcessingValidationBehavior(b.GetRequiredService<IReadOnlySettings>()),
                "Validates the API calls preventing calling ProcessAtomic if the Outbox is enabled.");

            endpointConfiguration.CustomDiagnosticsWriter(customDiagnosticsWriter);

            // 'WEBSITE_SITE_NAME' represents an Azure Function App and the environment variable is set when hosting the function in Azure.
            var functionAppName = configuration?.GetValue<string>("WEBSITE_SITE_NAME") ?? Environment.MachineName;
            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(functionAppName)
                .UsingCustomIdentifier(DeterministicGuid.Create(functionAppName));

            var licenseText = configuration?.GetValue<string>("NSERVICEBUS_LICENSE");
            if (!string.IsNullOrWhiteSpace(licenseText))
            {
                endpointConfiguration.License(licenseText);
            }

            TopicTopology topicTopology = TopicTopology.Default;
            var topologyOptionsSection = configuration?.GetSection("AzureServiceBus:TopologyOptions");
            if (topologyOptionsSection.Exists())
            {
                topicTopology = TopicTopology.FromOptions(topologyOptionsSection.Get<TopologyOptions>());
            }
            // Migration options take precedence over topology options. We are not doing additional checks here for now.
            var migrationOptionsSection = configuration?.GetSection("AzureServiceBus:MigrationTopologyOptions");
            if (migrationOptionsSection.Exists())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                topicTopology = TopicTopology.FromOptions(migrationOptionsSection.Get<MigrationTopologyOptions>());
#pragma warning restore CS0618 // Type or member is obsolete
            }

            Transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", topicTopology)
            {
                // This is required for the Outbox validation to work in NServiceBus 8. It does not affect the actual consistency mode because it is controlled by the functions
                // endpoint API (calling ProcessAtomic vs ProcessNonAtomic).
                TransportTransactionMode = TransportTransactionMode.ReceiveOnly
            };
            Routing = new RoutingSettings<AzureServiceBusTransport>(endpointConfiguration.GetSettings());

            endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

            AdvancedConfiguration = endpointConfiguration;
        }

        internal ServerlessTransport InitializeTransport()
        {
            var serverlessTransport = new ServerlessTransport(Transport, connectionString, connectionName);
            AdvancedConfiguration.UseTransport(serverlessTransport);
            return serverlessTransport;
        }

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public SerializationExtensions<T> UseSerialization<T>() where T : SerializationDefinition, new()
        {
            return AdvancedConfiguration.UseSerialization<T>();
        }

        /// <summary>
        /// Disables moving messages to the error queue even if an error queue name is configured.
        /// </summary>
        public void DoNotSendMessagesToErrorQueue() => recoverabilityPolicy.SendFailedMessagesToErrorQueue = false;

        /// <summary>
        /// Logs endpoint diagnostics information to the log. Diagnostics are logged on level <see cref="LogLevel.Info" />.
        /// </summary>
        public void LogDiagnostics() =>
            customDiagnosticsWriter = (diagnostics, cancellationToken) =>
            {
                LogManager.GetLogger("StartupDiagnostics").Info(diagnostics);
                return Task.CompletedTask;
            };

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
        readonly string connectionString;
        readonly string connectionName;
    }
}
