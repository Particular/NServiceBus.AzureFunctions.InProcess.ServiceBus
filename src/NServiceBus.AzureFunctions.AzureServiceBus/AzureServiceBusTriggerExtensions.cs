namespace NServiceBus.AzureFunctions.AzureServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Serverless;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// Extension methods for a ServerlessEndpoint when using AzureServiceBus triggers.
    /// </summary>
    public static class AzureServiceBusTriggerExtensions
    {
        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public static Task Process(this ServerlessEndpoint<ExecutionContext> endpoint, Message message, ExecutionContext executionContext)
        {
            var context = new MessageContext(
                Guid.NewGuid().ToString("N"),
                message.UserProperties.ToDictionary(x => x.Key, x => x.Value.ToString()),
                message.Body,
                new TransportTransaction(),
                new CancellationTokenSource(),
                new ContextBag());

            return endpoint.Process(context, executionContext);
        }
    }
}