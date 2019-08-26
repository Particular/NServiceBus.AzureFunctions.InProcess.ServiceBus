namespace NServiceBus.AzureFunctions.AzureServiceBus
{
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
    /// </summary>
    public class ServiceBusTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
        /// </summary>
        /// <param name="endpointName"></param>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName) : base(endpointName)
        {
        }
    }
}