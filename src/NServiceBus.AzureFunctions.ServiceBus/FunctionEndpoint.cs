namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Serverless;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<FunctionExecutionContext, ServiceBusTriggeredEndpointConfiguration>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public FunctionEndpoint(Func<FunctionExecutionContext, ServiceBusTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            var messageContext = CreateMessageContext(message);
            var functionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                await Process(messageContext, functionContext).ConfigureAwait(false);
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

                var errorHandleResult = await ProcessFailedMessage(errorContext, functionContext)
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
}
