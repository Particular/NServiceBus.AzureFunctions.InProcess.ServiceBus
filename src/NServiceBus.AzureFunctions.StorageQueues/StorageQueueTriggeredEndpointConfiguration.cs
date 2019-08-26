namespace NServiceBus.AzureFunctions.StorageQueues
{
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
    /// </summary>
    public class StorageQueueTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
        /// </summary>
        /// <param name="endpointName"></param>
        public StorageQueueTriggeredEndpointConfiguration(string endpointName) : base(endpointName)
        {
            UseTransport<AzureStorageQueueTransport>();
        }
    }
}