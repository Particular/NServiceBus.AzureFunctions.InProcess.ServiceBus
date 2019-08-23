namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
    /// </summary>
    public class AzureStorageQueueTriggerEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
        /// </summary>
        /// <param name="endpointName"></param>
        public AzureStorageQueueTriggerEndpointConfiguration(string endpointName) : base(endpointName)
        {
            //handle retries by native queue capabilities
            InMemoryRetries(0);
        }
    }
}