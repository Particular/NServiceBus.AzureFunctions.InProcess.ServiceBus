namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
    using IMessageReceiver = Microsoft.Azure.ServiceBus.Core.IMessageReceiver;

    class InProcessFunctionEndpoint : IFunctionEndpoint
    {
        public InProcessFunctionEndpoint(
            IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint,
            ServiceBusTriggeredEndpointConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        Task IFunctionEndpoint.Process(
            Message message,
            ExecutionContext executionContext,
            IMessageReceiver messageReceiver,
            ILogger functionsLogger,
            bool enableCrossEntityTransactions,
            CancellationToken cancellationToken) =>
            enableCrossEntityTransactions
                ? ProcessTransactional(message, executionContext, messageReceiver, functionsLogger, cancellationToken)
                : ProcessNonTransactional(message, executionContext, functionsLogger, cancellationToken);

        public async Task ProcessTransactional(Message message, ExecutionContext executionContext,
            IMessageReceiver messageReceiver, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken)
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

        public async Task ProcessNonTransactional(
            Message message,
            ExecutionContext executionContext,
            ILogger functionsLogger = null,
            CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken)
                .ConfigureAwait(false);

            await Process(message, NoTransactionStrategy.Instance, pipeline, cancellationToken)
                .ConfigureAwait(false);
        }

        internal static readonly string[] AssembliesToExcludeFromScanning = {
            "NCrontab.Signed.dll",
            "Azure.Core.dll",
            "Grpc.Core.Api.dll",
            "Grpc.Net.Common.dll",
            "Grpc.Net.Client.dll",
        "Grpc.Net.ClientFactory.dll"};

        internal static async Task Process(Message message, ITransactionStrategy transactionStrategy,
            PipelineInvoker pipeline, CancellationToken cancellationToken)
        {
            var messageId = message.GetMessageId();

            try
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var messageContext = new MessageContext(
                        messageId,
                        message.GetHeaders(),
                        message.Body,
                        transportTransaction,
                        pipeline.ReceiveAddress,
                        new ContextBag());

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
                        pipeline.ReceiveAddress,
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
        }

        async Task InitializeEndpointIfNecessary(ExecutionContext executionContext, ILogger logger, CancellationToken cancellationToken)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        var functionExecutionContext = new FunctionExecutionContext(executionContext, logger);
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

        public async Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Send(message, new SendOptions(), executionContext, functionsLogger, cancellationToken);
        }

        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Send(messageConstructor, new SendOptions(), executionContext, functionsLogger, cancellationToken);
        }

        public async Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(message, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Publish(message, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
        }

        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Publish(messageConstructor, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
        }

        public async Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Subscribe(eventType, new SubscribeOptions(), executionContext, functionsLogger, cancellationToken);
        }

        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        public Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
        {
            return Unsubscribe(eventType, new UnsubscribeOptions(), executionContext, functionsLogger, cancellationToken);
        }

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;

        readonly Func<FunctionExecutionContext, Task<IEndpointInstance>> endpointFactory;
        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        readonly ServiceBusTriggeredEndpointConfiguration configuration;
    }
}