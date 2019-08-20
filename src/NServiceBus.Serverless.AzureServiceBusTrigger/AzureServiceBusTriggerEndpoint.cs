namespace NServiceBus.AzureFunctions.AzureServiceBus
{
	using NServiceBus.Serverless;

	/// <summary>
	/// Represents a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
	/// </summary>
    public class AzureServiceBusTriggerEndpoint : ServerlessEndpointConfiguration
    {
		/// <summary>
		/// Creates a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
		/// </summary>
		/// <param name="endpointName"></param>
		public AzureServiceBusTriggerEndpoint(string endpointName) : base(endpointName)
        {
            //handle retries by native queue capabilities
            InMemoryRetries(0);
        }
    }
}