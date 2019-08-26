namespace NServiceBus.AzureFunctions.StorageQueues
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
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
        /// <param name="endpointName"></param>
        /// <param name="connectionStringName"></param>
        /// <param name="executionContext"></param>
        public StorageQueueTriggeredEndpointConfiguration(string endpointName, string connectionStringName, ExecutionContext executionContext) : base(endpointName)
        {
            Transport = UseTransport<AzureStorageQueueTransport>();

            var config = new ConfigurationBuilder()
                .SetBasePath(executionContext.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true)
                .Build();
            Transport.ConnectionString(config.GetValue<string>($"Values:{connectionStringName}"));
        }
    }
}