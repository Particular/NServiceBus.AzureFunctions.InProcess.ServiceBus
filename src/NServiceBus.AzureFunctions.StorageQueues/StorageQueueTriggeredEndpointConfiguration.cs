namespace NServiceBus.AzureFunctions.StorageQueues
{
    using Microsoft.Azure.WebJobs;
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

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
        /// </summary>
        public StorageQueueTriggeredEndpointConfiguration(ExecutionContext executionContext, string connectionStringName = "AzureWebJobsStorage") : base(executionContext.FunctionName)
        {
            Transport = UseTransport<AzureStorageQueueTransport>();

            var connectionString = System.Environment.GetEnvironmentVariable(connectionStringName);
            Transport.ConnectionString(connectionString);
        }
    }
}