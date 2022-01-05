namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
<<<<<<< HEAD
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
=======
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;
>>>>>>> 6fca7ee (Update to Microsoft.Azure.WebJobs.Extensions.ServiceBus 5.2.0 (#393))
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public interface IFunctionEndpoint
    {
        /// <summary>
<<<<<<< HEAD
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. This method will lookup the <see cref="ServiceBusTriggerAttribute.AutoComplete"/> setting to determine whether to use transactional or non-transactional processing.
        /// </summary>
        Task Process(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger = null);

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Process(Message, ExecutionContext, IMessageReceiver, ILogger)",
            TreatAsErrorFromVersion = "2",
            RemoveInVersion = "3")]
        Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null);
=======
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
>>>>>>> 6fca7ee (Update to Microsoft.Azure.WebJobs.Extensions.ServiceBus 5.2.0 (#393))

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