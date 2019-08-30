namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Logging;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Serverless;
    using System;
    using System.Diagnostics;
    using System.Reflection;
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

        internal FunctionsLoggerFactory FunctionsLoggerFactory { get; }

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

            FunctionsLoggerFactory = new FunctionsLoggerFactory(logger);
            LogManager.UseFactory(FunctionsLoggerFactory);
        }

        /// <summary>
        /// Attempts to derive the required configuration parameters automatically from the Azure Functions related attributes via reflection.
        /// </summary>
        public static ServiceBusTriggeredEndpointConfiguration AutoConfigure()
        {
            var configuration = TryGetTriggerConfiguration();
            if (configuration != null)
            {
                return new ServiceBusTriggeredEndpointConfiguration(configuration.QueueName, NullLogger.Instance, configuration.Connection);
            }

            throw new Exception($"Unable to automatically derive the endpoint name from the ServiceBusTrigger attribute. Make sure the attribute exists or create the {nameof(ServiceBusTriggeredEndpointConfiguration)} with the required parameter manually.");

            ServiceBusTriggerAttribute TryGetTriggerConfiguration()
            {
                var frames = new StackTrace().GetFrames();
                foreach (var stackFrame in frames)
                {
                    var method = stackFrame.GetMethod();
                    var functionAttribute = method.GetCustomAttribute<FunctionNameAttribute>(false);
                    if (functionAttribute != null)
                    {
                        foreach (var parameter in method.GetParameters())
                        {
                            var triggerConfiguration = parameter.GetCustomAttribute<ServiceBusTriggerAttribute>(false);
                            if (triggerConfiguration != null)
                            {
                                return triggerConfiguration;
                            }
                        }
                    }

                    return null;
                }

                return null;
            }
        }
    }
}