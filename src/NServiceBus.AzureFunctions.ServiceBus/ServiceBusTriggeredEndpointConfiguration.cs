namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
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
        /// <param name="endpointName"></param>
        /// <param name="connectionStringName"></param>
        /// <param name="executionContext"></param>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName, ExecutionContext executionContext) : base(endpointName)
        {
            Transport = UseTransport<AzureServiceBusTransport>();

            var config = new ConfigurationBuilder()
                .SetBasePath(executionContext.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true)
                .Build();
            Transport.ConnectionString(config.GetValue<string>($"Values:{connectionStringName}"));
        }
    }
}