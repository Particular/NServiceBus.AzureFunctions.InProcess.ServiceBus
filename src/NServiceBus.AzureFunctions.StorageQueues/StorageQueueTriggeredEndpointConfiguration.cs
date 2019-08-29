namespace NServiceBus.AzureFunctions.StorageQueues
{
    using NServiceBus.Logging;
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
    /// </summary>
    public class StorageQueueTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Azure Storage Queues transport
        /// </summary>
        public TransportExtensions<AzureStorageQueueTransport> Transport { get; }

        internal FunctionsLoggerFactory FunctionsLoggerFactory { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
        /// </summary>
        public StorageQueueTriggeredEndpointConfiguration(string endpointName, string connectionStringName = "AzureWebJobsStorage") : base(endpointName)
        {
            Transport = UseTransport<AzureStorageQueueTransport>();

            var connectionString = System.Environment.GetEnvironmentVariable(connectionStringName);
            Transport.ConnectionString(connectionString);

            var recoverability = AdvancedConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(4));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));

            FunctionsLoggerFactory = new FunctionsLoggerFactory();
            LogManager.UseFactory(FunctionsLoggerFactory);
        }
    }
}