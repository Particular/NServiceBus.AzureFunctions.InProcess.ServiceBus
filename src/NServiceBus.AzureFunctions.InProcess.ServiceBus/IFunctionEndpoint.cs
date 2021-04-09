namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public interface IFunctionEndpoint
    {
        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null);
    }
}