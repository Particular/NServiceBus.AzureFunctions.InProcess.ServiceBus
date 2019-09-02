﻿namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Logging;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Serverless;
    using System;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint running within an AzureServiceBus trigger.
    /// </summary>
    public class ServiceBusTriggeredEndpointConfiguration : ServerlessEndpointConfiguration
    {
        const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";

        /// <summary>
        /// Azure Service Bus transport
        /// </summary>
        public TransportExtensions<AzureServiceBusTransport> Transport { get; }

        static ServiceBusTriggeredEndpointConfiguration()
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint running within an Azure Service Bus trigger.
        /// </summary>
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, ILogger logger, string connectionStringName = null) : base(endpointName)
        {
            Transport = UseTransport<AzureServiceBusTransport>();

            var connectionString = Environment.GetEnvironmentVariable(connectionStringName ?? DefaultServiceBusConnectionName);
            Transport.ConnectionString(connectionString);

            var recoverability = AdvancedConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));
        }

        /// <summary>
        /// Attempts to derive the required configuration parameters automatically from the Azure Functions related attributes via reflection.
        /// </summary>
        public static ServiceBusTriggeredEndpointConfiguration FromAttributes(FunctionExecutionContext functionExecutionContext)
        {
            var configuration = TriggerDiscoverer.TryGet<ServiceBusTriggerAttribute>();
            if (configuration != null)
            {
                return new ServiceBusTriggeredEndpointConfiguration(configuration.QueueName, functionExecutionContext.Logger ?? NullLogger.Instance, configuration.Connection);
            }

            throw new Exception($"Unable to automatically derive the endpoint name from the ServiceBusTrigger attribute. Make sure the attribute exists or create the {nameof(ServiceBusTriggeredEndpointConfiguration)} with the required parameter manually.");
        }
    }
}