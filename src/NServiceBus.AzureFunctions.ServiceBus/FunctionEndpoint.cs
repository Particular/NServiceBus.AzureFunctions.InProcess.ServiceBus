using Microsoft.Extensions.DependencyInjection;

namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<ServiceBusTriggeredEndpointConfiguration>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IStartableEndpointWithExternallyManagedContainer startableEndpoint;

        /// <summary>
        /// ctor
        /// </summary>
        public FunctionEndpoint(IServiceProvider serviceProvider, IStartableEndpointWithExternallyManagedContainer startableEndpoint)
        {
            this.serviceProvider = serviceProvider;
            this.startableEndpoint = startableEndpoint;
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var messageContext = CreateMessageContext(message);
            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                await Process(messageContext, functionExecutionContext, serviceProvider, startableEndpoint).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var errorContext = new ErrorContext(
                    exception,
                    message.GetHeaders(),
                    messageContext.MessageId,
                    messageContext.Body,
                    new TransportTransaction(),
                    message.SystemProperties.DeliveryCount);

                var errorHandleResult = await ProcessFailedMessage(errorContext, functionExecutionContext, serviceProvider, startableEndpoint)
                    .ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    // return to signal to the Functions host it can complete the incoming message
                    return;
                }

                throw;
            }

            MessageContext CreateMessageContext(Message originalMessage)
            {
                return new MessageContext(
                    originalMessage.GetMessageId(),
                    originalMessage.GetHeaders(),
                    originalMessage.Body,
                    new TransportTransaction(),
                    new CancellationTokenSource(),
                    new ContextBag());
            }
        }
    }

    /// <summary>
    /// Register NServiceBus
    /// </summary>
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Register NServiceBus
        /// </summary>
        /// <param name="serviceCollection">service collection</param>
        /// <param name="configurationFactory">fac</param>
        /// <returns>Return service collection</returns>
        public static IServiceCollection UseNServiceBus(this IServiceCollection serviceCollection, Func<ServerlessEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            var startableEndpoint = EndpointWithExternallyManagedServiceProvider.Create(serviceBusTriggeredEndpointConfiguration.EndpointConfiguration, serviceCollection);

            serviceCollection.AddSingleton(serviceBusTriggeredEndpointConfiguration.PipelineInvoker);
            serviceCollection.AddSingleton(startableEndpoint);
            serviceCollection.AddSingleton<FunctionEndpoint>();

            return serviceCollection;
        }
    }
}
