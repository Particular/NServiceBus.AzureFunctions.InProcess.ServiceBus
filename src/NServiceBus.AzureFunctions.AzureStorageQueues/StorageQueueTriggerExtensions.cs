namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// Extension methods for a ServerlessEndpoint when using AzureStorageQueue triggers.
    /// </summary>
    public static class StorageQueueTriggerExtensions
    {
        /// <summary>
        /// Processes a message received from an AzureStorageQueue trigger using the NServiceBus message pipeline.
        /// </summary>
        public static Task Process(this FunctionEndpoint endpoint, CloudQueueMessage message, ExecutionContext executionContext)
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

            return endpoint.Process(messageContext, executionContext);
        }
    }
}