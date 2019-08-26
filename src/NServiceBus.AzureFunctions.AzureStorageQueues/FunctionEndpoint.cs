namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
    using Serverless;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Transports.WindowsAzureStorageQueues;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<ExecutionContext>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public FunctionEndpoint(Func<ExecutionContext, ServerlessEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }

        /// <summary>
        /// Processes a message received from an AzureStorageQueue trigger using the NServiceBus message pipeline.
        /// </summary>
        public Task Process(CloudQueueMessage message, ExecutionContext executionContext)
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

            return Process(messageContext, executionContext);
        }
    }
}