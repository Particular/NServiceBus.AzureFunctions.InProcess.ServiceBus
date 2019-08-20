namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
	using NServiceBus.Serverless;

	/// <summary>
	/// Represents a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
	/// </summary>
	public class AzureStorageQueueTriggerEndpoint : ServerlessEndpointConfiguration
    {
		/// <summary>
		/// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
		/// </summary>
		/// <param name="endpointName"></param>
		public AzureStorageQueueTriggerEndpoint(string endpointName) : base(endpointName)
        {
            //handle retries by native queue capabilities
            InMemoryRetries(0);
        }
    }
}