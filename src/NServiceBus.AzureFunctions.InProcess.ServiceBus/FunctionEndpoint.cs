namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
    using IMessageReceiver = Microsoft.Azure.ServiceBus.Core.IMessageReceiver;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public partial class FunctionEndpoint : IFunctionEndpoint
    {
        // This ctor is used for the FunctionsHostBuilder scenario where the endpoint is created already during configuration time using the function host's container.
        internal FunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint, ServiceBusTriggeredEndpointConfiguration configuration, IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. This method will lookup the <see cref="ServiceBusTriggerAttribute.AutoComplete"/> setting to determine whether to use transactional or non-transactional processing.
        /// </summary>
        Task IFunctionEndpoint.Process(Message message, IMessageReceiver messageReceiver, ILogger functionsLogger, CancellationToken cancellationToken) =>
            ReflectionHelper.GetAutoCompleteValue()
                ? ProcessNonTransactional(message, messageReceiver, functionsLogger, cancellationToken)
                : ProcessTransactional(message, messageReceiver, functionsLogger, cancellationToken);

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. This method will lookup the <see cref="ServiceBusTriggerAttribute.AutoComplete"/> setting to determine whether to use transactional or non-transactional processing.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        Task IFunctionEndpoint.Process(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger, CancellationToken cancellationToken) =>
            ReflectionHelper.GetAutoCompleteValue()
                ? ProcessNonTransactional(message, executionContext, messageReceiver, functionsLogger, cancellationToken)
                : ProcessTransactional(message, executionContext, messageReceiver, functionsLogger, cancellationToken);

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. All messages are committed transactionally with the successful processing of the incoming message.
        /// <remarks>Requires <see cref="ServiceBusTriggerAttribute.AutoComplete"/> to be set to false!</remarks>
        /// </summary>
        public async Task ProcessTransactional(Message message,
            IMessageReceiver messageReceiver, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(functionsLogger, cancellationToken)
                    .ConfigureAwait(false);

                await Process(message, new MessageReceiverTransactionStrategy(message, messageReceiver), pipeline, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // abandon message outside of a transaction scope to ensure the abandon operation can't be rolled back
                await messageReceiver.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. All messages are committed transactionally with the successful processing of the incoming message.
        /// <remarks>Requires <see cref="ServiceBusTriggerAttribute.AutoComplete"/> to be set to false!</remarks>
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task ProcessTransactional(Message message, ExecutionContext executionContext,
            IMessageReceiver messageReceiver, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => ProcessTransactional(message, messageReceiver, functionsLogger, cancellationToken);

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task ProcessNonTransactional(Message message,
            IMessageReceiver messageReceiver, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken)
                .ConfigureAwait(false);

            await Process(message, NoTransactionStrategy.Instance, pipeline, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task ProcessNonTransactional(Message message, ExecutionContext executionContext,
            IMessageReceiver messageReceiver, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => ProcessNonTransactional(message, messageReceiver, functionsLogger, cancellationToken);

        internal static readonly string[] AssembliesToExcludeFromScanning = { "NCrontab.Signed.dll" };

        internal static async Task Process(Message message, ITransactionStrategy transactionStrategy,
            PipelineInvoker pipeline, CancellationToken cancellationToken)
        {
            var messageId = message.GetMessageId();

            try
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var messageContext = CreateMessageContext(transportTransaction);

                    await pipeline.PushMessage(messageContext, cancellationToken).ConfigureAwait(false);

                    await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                    transaction?.Commit();
                }
            }
            catch (Exception exception)
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var errorContext = new ErrorContext(
                        exception,
                        message.GetHeaders(),
                        messageId,
                        message.Body,
                        transportTransaction,
                        message.SystemProperties.DeliveryCount,
                        new ContextBag());

                    var errorHandleResult = await pipeline.PushFailedMessage(errorContext, cancellationToken).ConfigureAwait(false);

                    if (errorHandleResult == ErrorHandleResult.Handled)
                    {
                        await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                        transaction?.Commit();
                        return;
                    }

                    throw;
                }
            }

            MessageContext CreateMessageContext(TransportTransaction transportTransaction) =>
                new MessageContext(
                    messageId,
                    message.GetHeaders(),
                    message.Body,
                    transportTransaction,
                    new ContextBag());
        }

        async Task InitializeEndpointIfNecessary(ILogger logger, CancellationToken cancellationToken)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        var functionExecutionContext = new FunctionExecutionContext(logger);
                        endpoint = await endpointFactory(functionExecutionContext).ConfigureAwait(false);

                        pipeline = configuration.PipelineInvoker;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        /// <inheritdoc />
        public async Task Send(object message, SendOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(message, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(message, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Send(object message, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(message, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(messageConstructor, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(messageConstructor, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Send(messageConstructor, new SendOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public async Task Publish(object message, PublishOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(message, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(message, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(message, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Publish(object message, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(message, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(messageConstructor, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(messageConstructor, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Publish<T>(Action<T> messageConstructor, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Publish(messageConstructor, new PublishOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, SubscribeOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Subscribe(eventType, new SubscribeOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Subscribe(eventType, new SubscribeOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Subscribe(Type eventType, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Subscribe(eventType, new SubscribeOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Unsubscribe(eventType, options, functionsLogger, cancellationToken);

        /// <inheritdoc />
        [ObsoleteEx(
            TreatAsErrorFromVersion = "3",
            RemoveInVersion = "4",
            Message = "Use the overload without the ExecutionContext parameter")]
        public Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Unsubscribe(eventType, new UnsubscribeOptions(), functionsLogger, cancellationToken);

        /// <inheritdoc />
        public Task Unsubscribe(Type eventType, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
            => Unsubscribe(eventType, new UnsubscribeOptions(), functionsLogger, cancellationToken);

        readonly Func<FunctionExecutionContext, Task<IEndpointInstance>> endpointFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        ServiceBusTriggeredEndpointConfiguration configuration;

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;
    }
}