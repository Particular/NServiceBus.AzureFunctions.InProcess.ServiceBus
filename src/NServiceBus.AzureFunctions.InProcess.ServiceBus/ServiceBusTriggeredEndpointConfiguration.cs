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
    public class ServiceBusTriggeredEndpointConfiguration
    {
        static ServiceBusTriggeredEndpointConfiguration()
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(IConfiguration configuration)
            : this(GetConfiguredValueOrFallback(configuration, "ENDPOINT_NAME", optional: false), configuration)
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null)
            : this(endpointName, null, configuration)
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null)
            : this(endpointName, connectionStringName, null)
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName)
            : this(endpointName, null, null)
        {
        }

        // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
        Func<string, CancellationToken, Task> customDiagnosticsWriter = (_, __) => Task.CompletedTask;
        string endpointName;
        IConfiguration configuration;
        string connectionString;
        string connectionStringName;
        bool sendFailedMessagesToErrorQueue = true;

        Action<EndpointConfiguration> setSerializer = endpointConfig =>
            endpointConfig.UseSerialization<NewtonsoftSerializer>();

        Action<RoutingSettings> configureRouting;

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
        /// Configure the ServiceBus connection string used to send messages.
        /// </summary>
        public void ServiceBusConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
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
                connectionString = GetConfiguredValueOrFallback(configuration, connectionStringName ?? DefaultServiceBusConnectionName, optional: false);
            }

            var transport = new AzureServiceBusTransport(connectionString);
            serverlessTransport = new ServerlessTransport(transport);

            var routing = endpointConfiguration.UseTransport(serverlessTransport);

            configureRouting?.Invoke(routing);

            setSerializer(endpointConfiguration);

            customConfiguration?.Invoke(endpointConfiguration);

            return endpointConfiguration;
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null, IConfiguration configuration = null)
        {
            this.endpointName = endpointName;
            this.connectionStringName = connectionStringName;
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
        /// Attempts to derive the required configuration parameters automatically from the Azure Functions related attributes via
        /// reflection.
        /// </summary>
        [ObsoleteEx(
            Message = "The static hosting model has been deprecated. Refer to the documentation for details on how to use class-instance approach instead.",
            RemoveInVersion = "3",
            TreatAsErrorFromVersion = "2")]
        public static ServiceBusTriggeredEndpointConfiguration FromAttributes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public SerializationExtensions<T> UseSerialization<T>() where T : SerializationDefinition, new()
        {
            setSerializer = config => config.UseSerialization<T>();
            // TODO: Figure this out
            return default;
            //return EndpointConfiguration.UseSerialization<T>();
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
    }
}
