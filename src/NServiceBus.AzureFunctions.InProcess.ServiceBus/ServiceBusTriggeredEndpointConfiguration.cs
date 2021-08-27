namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Configuration.AdvancedExtensibility;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Serialization;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint.
    /// </summary>
    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        static ServiceBusTriggeredEndpointConfiguration()
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

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
        /// Azure Service Bus connection string used to send  messages.
        /// </summary>
        public string ServiceBusConnectionString
        {
            get => connectionString;
            set
            {
                Guard.AgainstNullAndEmpty(nameof(value), value);
                connectionString = value;
                Transport.ChangeConnectionString(value);
            }
        }

        string connectionString;

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);

            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            recoverability.CustomPolicy(recoverabilityPolicy.Invoke);

            endpointConfiguration.CustomDiagnosticsWriter(customDiagnosticsWriter);

            // 'WEBSITE_SITE_NAME' represents an Azure Function App and the environment variable is set when hosting the function in Azure.
            var functionAppName = GetConfiguredValueOrFallback(configuration, "WEBSITE_SITE_NAME", true) ?? Environment.MachineName;
            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(functionAppName)
                .UsingCustomIdentifier(DeterministicGuid.Create(functionAppName));

            var licenseText = GetConfiguredValueOrFallback(configuration, "NSERVICEBUS_LICENSE", optional: true);
            if (!string.IsNullOrWhiteSpace(licenseText))
            {
                endpointConfiguration.License(licenseText);
            }

            connectionString = GetConfiguredValueOrFallback(configuration, DefaultServiceBusConnectionName, optional: true);
            Transport = new AzureServiceBusTransport(connectionString ?? "missing"); // connection string will be checked before creating the startable endpoint as it might be configured within UseNServiceBus().

            serverlessTransport = new ServerlessTransport(Transport);
            var serverlessRouting = endpointConfiguration.UseTransport(serverlessTransport);
            Routing = new RoutingSettings<AzureServiceBusTransport>(serverlessRouting.GetSettings());

            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

            AdvancedConfiguration = endpointConfiguration;
        }

        static string GetConfiguredValueOrFallback(IConfiguration configuration, string key, bool optional)
        {
            if (configuration != null)
            {
                var configuredValue = configuration.GetValue<string>(key);
                if (!string.IsNullOrWhiteSpace(configuredValue))
                {
                    return configuredValue;
                }
            }

            var environmentVariable = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(environmentVariable) && !optional)
            {
                throw new Exception($"Configuration or environment value for '{key}' was not set or was empty.");
            }
            return environmentVariable;
        }

        internal PipelineInvoker PipelineInvoker => serverlessTransport.PipelineInvoker;

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

        ServerlessTransport serverlessTransport;
        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";
    }
}
