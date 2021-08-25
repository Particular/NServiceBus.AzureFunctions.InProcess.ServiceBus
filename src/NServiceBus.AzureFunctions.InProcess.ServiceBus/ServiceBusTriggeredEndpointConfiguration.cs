namespace NServiceBus
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Logging;
    using Microsoft.Azure.WebJobs.Description;
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
        bool sendFailedMessagesToErrorQueue = true;

        /// <summary>
        /// 
        /// </summary>
        public AzureServiceBusTransport Transport { get; }

        /// <summary>
        /// 
        /// </summary>
        public RoutingSettings Routing { get; }

        /// <summary>
        /// 
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration { get; }

        /// <summary>
        /// 
        /// </summary>
        public string ServiceBusConnectionString
        {
            get => connectionString;
            set
            {
                //TODO throw if nullofwhitespace
                connectionString = value;
                var property = typeof(AzureServiceBusTransport).GetProperty("ConnectionString", BindingFlags.Instance | BindingFlags.NonPublic); //TODO use correct bindingflags
                property.SetValue(Transport, connectionString);
            }
        }

        private string connectionString;

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration)
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

            connectionString = GetConfiguredValueOrFallback(configuration, DefaultServiceBusConnectionName, optional: true);
            Transport = new AzureServiceBusTransport(connectionString ?? "missing");


            serverlessTransport = new ServerlessTransport(Transport);
            Routing = endpointConfiguration.UseTransport(serverlessTransport);

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
