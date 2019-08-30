namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Microsoft.Extensions.Logging;
    using Logging;
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
    /// </summary>
    public class ServiceBusTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Azure Service Bus transport
        /// </summary>
        public TransportExtensions<AzureServiceBusTransport> Transport { get; }

        internal FunctionsLoggerFactory FunctionsLoggerFactory { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an Azure Service Bus trigger.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, ILogger logger, string connectionStringName = "AzureWebJobsServiceBus") : base(endpointName)
        {
            Transport = UseTransport<AzureServiceBusTransport>();

            var connectionString = System.Environment.GetEnvironmentVariable(connectionStringName);
            Transport.ConnectionString(connectionString);

            var recoverability = AdvancedConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));

            FunctionsLoggerFactory = new FunctionsLoggerFactory(logger);
            LogManager.UseFactory(FunctionsLoggerFactory);
        }
    }
}