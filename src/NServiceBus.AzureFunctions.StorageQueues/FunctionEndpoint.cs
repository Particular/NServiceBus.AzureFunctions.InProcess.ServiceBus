namespace NServiceBus.AzureFunctions.StorageQueues
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Transports.WindowsAzureStorageQueues;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Serverless;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<ExecutionContext, StorageQueueTriggeredEndpointConfiguration>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public FunctionEndpoint(Func<ExecutionContext, StorageQueueTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }

        /// <summary>
        /// Processes a message received from an AzureStorageQueue trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(CloudQueueMessage message, ExecutionContext executionContext)
        {
            var serializer = new JsonSerializer();
            var msg = serializer.Deserialize<MessageWrapper>(
                new JsonTextReader(new StreamReader(new MemoryStream(message.AsBytes))));

            var messageContext = new MessageContext(
                Guid.NewGuid().ToString("N"),
                msg.Headers,
                msg.Body,
                new TransportTransaction(),
                new CancellationTokenSource(),
                new ContextBag());

            try
            {
                await Process(messageContext, executionContext).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var errorContext = new ErrorContext(
                    exception,
                    new Dictionary<string, string>(messageContext.Headers),
                    messageContext.MessageId,
                    messageContext.Body,
                    new TransportTransaction(), 
                    message.DequeueCount);

                var errorHandleResult = await ProcessFailedMessage(errorContext, executionContext)
                    .ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    // return to signal to the Functions host it can complete the incoming message
                    return;
                }

                throw;
            }
        }
    }
}