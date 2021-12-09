namespace NServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extension metods for <see cref="IFunctionEndpoint"/>.
    /// </summary>
    public static class IFunctionEndpointExtensions
    {
        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public static Task Process(
            this IFunctionEndpoint functionEndpoint,
            Message message,
            ExecutionContext executionContext,
            IMessageReceiver messageReceiver,
            bool enableCrossEntityTransactions,
            ILogger functionsLogger = null)
        {
            var endpoint = (FunctionEndpoint)functionEndpoint;

            if (enableCrossEntityTransactions)
            {
                return endpoint.ProcessTransactional(message, executionContext, messageReceiver, functionsLogger);
            }

            return endpoint.ProcessNonTransactional(message, executionContext, messageReceiver, functionsLogger);
        }
    }
}