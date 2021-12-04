namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Logging;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// Allows Azure ServiceBus messages to be processed by the NServiceBus endpoint.
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        Task Process(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, bool enableCrossEntityTransactions, ILogger functionsLogger = null, CancellationToken cancellationToken = default);
    }
}