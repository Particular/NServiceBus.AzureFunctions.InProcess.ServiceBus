namespace NServiceBus.Serverless.AzureServiceBusTrigger
{
    public class AzureServiceBusTriggerEndpoint : ServerlessEndpointConfiguration
    {
        public AzureServiceBusTriggerEndpoint(string endpointName) : base(endpointName)
        {
            //handle retries by native queue capabilities
            InMemoryRetries(0);
        }
    }
}