namespace NServiceBus
{
    using System;
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

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null, IConfiguration configuration = null)
        {
            EndpointConfiguration = new EndpointConfiguration(endpointName);

            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            EndpointConfiguration.Recoverability().CustomPolicy(recoverabilityPolicy.Invoke);

            // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
            // Can be overriden by ServerlessEndpointConfiguration.LogDiagnostics().
            EndpointConfiguration.CustomDiagnosticsWriter((_, __) => Task.CompletedTask);

            // 'WEBSITE_SITE_NAME' represents an Azure Function App and the environment variable is set when hosting the function in Azure.
            var functionAppName = GetConfiguredValueOrFallback(configuration, "WEBSITE_SITE_NAME", true) ?? Environment.MachineName;
            EndpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(functionAppName)
                .UsingCustomIdentifier(DeterministicGuid.Create(functionAppName));

            var licenseText = GetConfiguredValueOrFallback(configuration, "NSERVICEBUS_LICENSE", optional: true);
            if (!string.IsNullOrWhiteSpace(licenseText))
            {
                EndpointConfiguration.License(licenseText);
            }

            var connectionString = GetConfiguredValueOrFallback(configuration, connectionStringName ?? DefaultServiceBusConnectionName, optional: true);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                // TODO: What do we do if we don't have a connectionString?
                UseTransport(new AzureServiceBusTransport(connectionString));
            }
            else if (!string.IsNullOrWhiteSpace(connectionStringName))
            {
                throw new Exception($"Azure Service Bus connection string named '{connectionStringName}' was provided but wasn't found in the environment variables. Make sure the connection string is stored in the environment variable named '{connectionStringName}'.");
            }

            var recoverability = AdvancedConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));

            EndpointConfiguration.UseSerialization<NewtonsoftSerializer>();
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

        /// <summary>
        /// Azure Service Bus transport
        /// </summary>
        public AzureServiceBusTransport Transport { get; }

        internal EndpointConfiguration EndpointConfiguration { get; }
        internal PipelineInvoker PipelineInvoker { get; private set; }

        /// <summary>
        /// Gives access to the underlying endpoint configuration for advanced configuration options.
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration => EndpointConfiguration;

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
        /// Define a transport to be used when sending and publishing messages.
        /// </summary>
        protected AzureServiceBusTransport UseTransport(AzureServiceBusTransport transport)
        {
            var serverlessTransport = new ServerlessTransport(transport);
            EndpointConfiguration.UseTransport(serverlessTransport);
            PipelineInvoker = serverlessTransport.PipelineInvoker;
            return transport;
        }

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public SerializationExtensions<T> UseSerialization<T>() where T : SerializationDefinition, new()
        {
            return EndpointConfiguration.UseSerialization<T>();
        }

        /// <summary>
        /// Disables moving messages to the error queue even if an error queue name is configured.
        /// </summary>
        public void DoNotSendMessagesToErrorQueue()
        {
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = false;
        }

        /// <summary>
        /// Logs endpoint diagnostics information to the log. Diagnostics are logged on level <see cref="LogLevel.Info" />.
        /// </summary>
        public void LogDiagnostics()
        {
            EndpointConfiguration.CustomDiagnosticsWriter((diagnostics, cancellationToken) =>
            {
                LogManager.GetLogger("StartupDiagnostics").Info(diagnostics);
                return Task.CompletedTask;
            });
        }

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";
    }
}
