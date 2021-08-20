namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
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
        readonly string endpointName;
        readonly IConfiguration configuration;
        string connectionString;
        string connectionStringName;
        bool sendFailedMessagesToErrorQueue = true;

        ISerializationConfigurationStrategy serializationConfigurationStrategy = new SerializationConfigurationStrategy<NewtonsoftSerializer>();
        Action<RoutingSettings> configureRouting;
        Action<AzureServiceBusTransport> configureTransport;
        Action<EndpointConfiguration> customConfiguration;


        /// <summary>
        /// Configure the underlying Endpoint Configuration directly.
        /// </summary>
        public void Advanced(Action<EndpointConfiguration> customConfiguration)
        {
            this.customConfiguration = customConfiguration;
        }

        /// <summary>
        /// Configure message routing.
        /// </summary>
        public void Routing(Action<RoutingSettings> configureRouting)
        {
            this.configureRouting = configureRouting;
        }

        /// <summary>
        /// Configure the key used to look up the ServiceBus connection string in configuration or environment variables.
        /// </summary>
        public void ServiceBusConnectionStringName(string connectionStringName)
        {
            this.connectionStringName = connectionStringName;
        }

        /// <summary>
        /// Configure the ServiceBus connection string used to send messages.
        /// </summary>
        public void ServiceBusConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Apply custom configuration to the NServiceBus Azure Service Bus transport.
        /// </summary>
        public void ConfigureTransport(Action<AzureServiceBusTransport> configureTransport)
        {
            this.configureTransport = configureTransport;
        }

        internal EndpointConfiguration CreateEndpointConfiguration()
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);
            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = sendFailedMessagesToErrorQueue;
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

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (string.IsNullOrWhiteSpace(connectionStringName))
                {
                    connectionString = GetConfiguredValueOrFallback(configuration, DefaultServiceBusConnectionName, optional: true);
                }
                else
                {
                    connectionString = GetConfiguredValueOrFallback(configuration, connectionStringName, optional: false);
                }
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception($@"Azure Service Bus connection string has not been configured. Specify a connection string through IConfiguration, an environment variable named {DefaultServiceBusConnectionName} or using:
  `serviceBusTriggeredEndpointConfiguration.{nameof(ServiceBusConnectionString)}(connectionString);`");
            }

            var transport = new AzureServiceBusTransport(connectionString);
            configureTransport?.Invoke(transport);
            serverlessTransport = new ServerlessTransport(transport);
            var routing = endpointConfiguration.UseTransport(serverlessTransport);

            configureRouting?.Invoke(routing);

            serializationConfigurationStrategy.ApplyTo(endpointConfiguration);

            customConfiguration?.Invoke(endpointConfiguration);

            return endpointConfiguration;
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration)
        {
            this.endpointName = endpointName;
            this.configuration = configuration;
        }

        static string GetConfiguredValueOrFallback(IConfiguration configuration, string key, bool optional)
        {
            if (configuration != null)
            {
                var configuredValue = configuration.GetValue<string>(key);
                if (configuredValue != null)
                {
                    return configuredValue;
                }
            }

            var environmentVariable = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(environmentVariable) && !optional)
            {
                throw new Exception($"Configuration or environment value for '{key}' was not set or was empty.");
            }
            return environmentVariable;
        }

        internal PipelineInvoker PipelineInvoker => serverlessTransport.PipelineInvoker;

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public void UseSerialization<T>(Action<SerializationExtensions<T>> advancedConfiguration = null) where T : SerializationDefinition, new()
        {
            serializationConfigurationStrategy = new SerializationConfigurationStrategy<T>(advancedConfiguration);
        }

        /// <summary>
        /// Disables moving messages to the error queue even if an error queue name is configured.
        /// </summary>
        public void DoNotSendMessagesToErrorQueue() => sendFailedMessagesToErrorQueue = false;

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

        interface ISerializationConfigurationStrategy
        {
            void ApplyTo(EndpointConfiguration endpointConfiguration);
        }

        class SerializationConfigurationStrategy<T> : ISerializationConfigurationStrategy where T : SerializationDefinition, new()
        {
            readonly Action<SerializationExtensions<T>> configurationAction;

            public SerializationConfigurationStrategy(Action<SerializationExtensions<T>> configurationAction = null)
            {
                this.configurationAction = configurationAction;
            }

            public void ApplyTo(EndpointConfiguration endpointConfiguration)
            {
                var serializationSettings = endpointConfiguration.UseSerialization<T>();
                configurationAction?.Invoke(serializationSettings);
            }
        }
    }
}
