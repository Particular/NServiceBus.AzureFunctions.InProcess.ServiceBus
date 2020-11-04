using Microsoft.Extensions.DependencyInjection;

namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Logging;
    using Serialization;
    using Transport;

    /// <summary>
    /// The configuration for an NServiceBus endpoint optimized for serverless environments.
    /// </summary>
    public abstract class ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        protected ServerlessEndpointConfiguration(string endpointName)
        {
            EndpointConfiguration = new EndpointConfiguration(endpointName);

            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            EndpointConfiguration.Recoverability().CustomPolicy(recoverabilityPolicy.Invoke);

            // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
            // Can be overriden by ServerlessEndpointConfiguration.LogDiagnostics().
            EndpointConfiguration.CustomDiagnosticsWriter(_ => Task.CompletedTask);

            // 'WEBSITE_SITE_NAME' represents an Azure Function App and the environment variable is set when hosting the function in Azure.
            var functionAppName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? Environment.MachineName;
            EndpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(functionAppName)
                .UsingCustomIdentifier(DeterministicGuid.Create(functionAppName));

            // Look for license as an environment variable
            var licenseText = Environment.GetEnvironmentVariable("NSERVICEBUS_LICENSE");
            if (!string.IsNullOrWhiteSpace(licenseText))
            {
               EndpointConfiguration.License(licenseText);
            }
        }

        internal EndpointConfiguration EndpointConfiguration { get; }

        internal PipelineInvoker PipelineInvoker { get; private set; }

        /// <summary>
        /// Gives access to the underlying endpoint configuration for advanced configuration options.
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration => EndpointConfiguration;

        /// <summary>
        /// Define a transport to be used when sending and publishing messages.
        /// </summary>
        protected TransportExtensions<TTransport> UseTransport<TTransport>()
            where TTransport : TransportDefinition, new()
        {
            var serverlessTransport = EndpointConfiguration.UseTransport<ServerlessTransport<TTransport>>();

            PipelineInvoker = serverlessTransport.PipelineAccess();
            return serverlessTransport.BaseTransportConfiguration();
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
        /// Logs endpoint diagnostics information to the log. Diagnostics are logged on level <see cref="LogLevel.Info"/>.
        /// </summary>
        public void LogDiagnostics()
        {
            EndpointConfiguration.CustomDiagnosticsWriter(diagnostics =>
            {
                LogManager.GetLogger("StartupDiagnostics").Info(diagnostics);
                return Task.CompletedTask;
            });
        }

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
    }

    static class DeterministicGuid
    {
        public static Guid Create(string data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(data);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}
