namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Microsoft.Azure.WebJobs;
    using Serverless;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
    /// </summary>
    public class ServiceBusTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Azure Service Bus transport
        /// </summary>
        public TransportExtensions<AzureServiceBusTransport> Transport { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an Azure Service Bus trigger.
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <param name="executionContext"></param>
        public ServiceBusTriggeredEndpointConfiguration(string connectionStringName, ExecutionContext executionContext) : base(executionContext.FunctionName)
        {
            Transport = UseTransport<AzureServiceBusTransport>();

            var connectionString = System.Environment.GetEnvironmentVariable(connectionStringName);
            Transport.ConnectionString(connectionString);
        }
    }
}