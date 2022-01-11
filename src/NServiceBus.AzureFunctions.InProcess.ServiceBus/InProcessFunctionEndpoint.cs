namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Azure.Messaging.ServiceBus;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// TODO
    /// </summary>
    public class InProcessFunctionEndpoint : IFunctionEndpoint
    {
        internal InProcessFunctionEndpoint(
            IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint,
            ServiceBusTriggeredEndpointConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        async Task IFunctionEndpoint.Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Send(object message, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Send(message, new SendOptions(), executionContext, functionsLogger, cancellationToken);
        }

        async Task IFunctionEndpoint.Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Send(messageConstructor, new SendOptions(), executionContext, functionsLogger, cancellationToken);
        }

        async Task IFunctionEndpoint.Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(message, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Publish(object message, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Publish(message, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
        }

        async Task IFunctionEndpoint.Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Publish(messageConstructor, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
        }

        async Task IFunctionEndpoint.Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Subscribe(eventType, new SubscribeOptions(), executionContext, functionsLogger, cancellationToken);
        }

        async Task IFunctionEndpoint.Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        Task IFunctionEndpoint.Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger, CancellationToken cancellationToken)
        {
            return ((IFunctionEndpoint)this).Unsubscribe(eventType, new UnsubscribeOptions(), executionContext, functionsLogger, cancellationToken);
        }


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="message"></param>
        /// <param name="executionContext"></param>
        /// <param name="functionsLogger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ProcessNonAtomic(
            object message,
            object executionContext,
            object functionsLogger = null,
            CancellationToken cancellationToken = default)
        {
            return ProcessNonAtomic((ServiceBusReceivedMessage)message, (ExecutionContext)executionContext, (ILogger)functionsLogger, cancellationToken);
        }

        async Task ProcessNonAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ILogger functionsLogger,
            CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var messageContext = CreateMessageContext(message, new TransportTransaction());

                await pipeline.PushMessage(messageContext, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception exception)
            {
                var errorContext = CreateErrorContext(message, new TransportTransaction(), exception);

                var errorHandleResult = await pipeline.PushFailedMessage(errorContext, cancellationToken).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return;
                }
                throw;
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="message"></param>
        /// <param name="executionContext"></param>
        /// <param name="serviceBusClient"></param>
        /// <param name="messageActions"></param>
        /// <param name="functionsLogger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ProcessAtomic(
            object message,
            object executionContext,
            object serviceBusClient,
            object messageActions,
            object functionsLogger = null,
            CancellationToken cancellationToken = default)
        {
            return ProcessAtomic((ServiceBusReceivedMessage)message, (ExecutionContext)executionContext, (ServiceBusClient)serviceBusClient, (ServiceBusMessageActions)messageActions, (ILogger)functionsLogger, cancellationToken);
        }

        async Task ProcessAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ServiceBusClient serviceBusClient,
            ServiceBusMessageActions messageActions,
            ILogger functionsLogger = null,
            CancellationToken cancellationToken = default)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await messageActions.AbandonMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                throw;
            }

            try
            {
                using (var transaction = CreateTransaction())
                {
                    var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                    var messageContext = CreateMessageContext(message, transportTransaction);

                    await pipeline.PushMessage(messageContext, cancellationToken).ConfigureAwait(false);

                    await SafeCompleteMessageAsync(messageActions, message, transaction, cancellationToken).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
            catch (Exception exception)
            {
                ErrorHandleResult result;
                using (var transaction = CreateTransaction())
                {
                    var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                    var errorContext = CreateErrorContext(message, transportTransaction, exception);

                    result = await pipeline.PushFailedMessage(errorContext, cancellationToken).ConfigureAwait(false);

                    if (result == ErrorHandleResult.Handled)
                    {
                        await SafeCompleteMessageAsync(messageActions, message, transaction, cancellationToken).ConfigureAwait(false);
                    }

                    transaction.Commit();
                }

                if (result != ErrorHandleResult.Handled)
                {
                    await messageActions.AbandonMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        ErrorContext CreateErrorContext(ServiceBusReceivedMessage message, TransportTransaction transportTransaction, Exception exception)
        {
            var errorContext = new ErrorContext(
                exception,
                message.GetHeaders(),
                message.MessageId,
                message.Body,
                transportTransaction,
                message.DeliveryCount,
                pipeline.ReceiveAddress,
                new ContextBag());
            return errorContext;
        }

        MessageContext CreateMessageContext(ServiceBusReceivedMessage message, TransportTransaction transportTransaction)
        {
            var messageContext = new MessageContext(
                message.MessageId,
                message.GetHeaders(),
                message.Body,
                transportTransaction,
                pipeline.ReceiveAddress,
                new ContextBag());
            return messageContext;
        }

        static TransportTransaction CreateTransportTransaction(string messagePartitionKey, CommittableTransaction transaction, ServiceBusClient serviceBusClient)
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(serviceBusClient);
            transportTransaction.Set("IncomingQueue.PartitionKey", messagePartitionKey);
            transportTransaction.Set(transaction);
            return transportTransaction;
        }

        static async Task SafeCompleteMessageAsync(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message, Transaction committableTransaction, CancellationToken cancellationToken = default)
        {
            using (var scope = new TransactionScope(committableTransaction, TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                scope.Complete();
            }
        }

        static CommittableTransaction CreateTransaction() =>
            new CommittableTransaction(new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TransactionManager.MaximumTimeout
            });

        internal static readonly string[] AssembliesToExcludeFromScanning = {
            "NCrontab.Signed.dll",
            "Azure.Core.dll",
            "Grpc.Core.Api.dll",
            "Grpc.Net.Common.dll",
            "Grpc.Net.Client.dll",
            "Grpc.Net.ClientFactory.dll",
            "Azure.Identity.dll",
            "Microsoft.Extensions.Azure.dll",
            "NServiceBus.Extensions.DependencyInjection.dll"
        };

        internal async Task InitializeEndpointIfNecessary(ExecutionContext executionContext, ILogger logger, CancellationToken cancellationToken)
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

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;

        readonly Func<FunctionExecutionContext, Task<IEndpointInstance>> endpointFactory;
        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        readonly ServiceBusTriggeredEndpointConfiguration configuration;
    }
}