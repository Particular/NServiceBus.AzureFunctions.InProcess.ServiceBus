namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.Extensions.Logging;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public interface IFunctionEndpoint
    {
        /// <summary>
        /// Processes the received message in atomic sends with receive mode.
        /// </summary>
        Task ProcessAtomic(
           ServiceBusReceivedMessage message,
           ExecutionContext executionContext,
           ServiceBusClient serviceBusClient,
           ServiceBusMessageActions messageActions,
           ILogger functionsLogger = null,
           CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the received message in receive only transaction mode.
        /// </summary>
        Task ProcessNonAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ILogger functionsLogger = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default);
    }
}