namespace NServiceBus.AzureFunctions.StorageQueues
{
    using Logging;
    using Microsoft.Extensions.Logging;
	using System;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging.Abstractions;
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
    /// </summary>
    public class StorageQueueTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        const string DefaultStorageConnectionString = "AzureWebJobsStorage";

        /// <summary>
        /// Azure Storage Queues transport
        /// </summary>
        public TransportExtensions<AzureStorageQueueTransport> Transport { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an AzureStorageQueue trigger.
        /// </summary>
        public StorageQueueTriggeredEndpointConfiguration(string endpointName, ILogger logger, string connectionStringName = null) : base(endpointName)
        {
            Transport = UseTransport<AzureStorageQueueTransport>();

            var connectionString = Environment.GetEnvironmentVariable(connectionStringName ?? DefaultStorageConnectionString);
            Transport.ConnectionString(connectionString);

            var recoverability = AdvancedConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(4));
            recoverability.Delayed(settings => settings.NumberOfRetries(0));

            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

        /// <summary>
        /// Attempts to derive the required configuration parameters automatically from the Azure Functions related attributes via reflection.
        /// </summary>
        public static StorageQueueTriggeredEndpointConfiguration FromAttributes(FunctionExecutionContext functionExecutionContext)
        {
            var configuration = TriggerDiscoverer.TryGet<QueueTriggerAttribute>();
            if (configuration != null)
            {
                return new StorageQueueTriggeredEndpointConfiguration(configuration.QueueName, functionExecutionContext.Logger ?? NullLogger.Instance, configuration.Connection);
            }

            throw new Exception($"Unable to automatically derive the endpoint name from the QueueTrigger attribute. Make sure the attribute exists or create the {nameof(StorageQueueTriggeredEndpointConfiguration)} with the required parameter manually.");
        }
    }
}