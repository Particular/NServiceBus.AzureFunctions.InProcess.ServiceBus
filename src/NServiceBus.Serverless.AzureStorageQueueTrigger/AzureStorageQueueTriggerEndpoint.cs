namespace NServiceBus.Serverless.AzureStorageQueueTrigger
{
    public class AzureStorageQueueTriggerEndpoint : ServerlessEndpointConfiguration
    {
        public AzureStorageQueueTriggerEndpoint(string endpointName) : base(endpointName)
        {
            //handle retries by native queue capabilities
            InMemoryRetries(0);
        }
    }
}