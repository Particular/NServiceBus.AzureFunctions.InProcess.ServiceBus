namespace NServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Process messages with the NServiceBus pipeline.
    /// </summary>
    public interface IAdvancedFunctionEndpoint
    {
        /// <summary>
        /// Process a message with the NServiceBus pipeline. If <paramref name="autoComplete"></paramref> is set to true then the message is processed transactionally.
        /// </summary>
        Task Process(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, bool autoComplete, ILogger logger);
    }
}