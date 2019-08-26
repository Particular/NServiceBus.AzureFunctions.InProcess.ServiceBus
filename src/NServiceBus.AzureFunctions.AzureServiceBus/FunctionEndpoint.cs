namespace NServiceBus.AzureFunctions.AzureServiceBus
{
    using Serverless;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
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
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public Task Process(Message message, ExecutionContext executionContext)
        {
            var context = new MessageContext(
                Guid.NewGuid().ToString("N"),
                message.UserProperties.ToDictionary(x => x.Key, x => x.Value.ToString()),
                message.Body,
                new TransportTransaction(),
                new CancellationTokenSource(),
                new ContextBag());

            return Process(context, executionContext);
        }
    }
}