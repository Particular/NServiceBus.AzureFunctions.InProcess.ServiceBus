﻿namespace NServiceBus
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

        public async Task ProcessNonAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ILogger functionsLogger = null,
            CancellationToken cancellationToken = default)
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

        public async Task ProcessAtomic(
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
                await InitializeEndpointIfNecessary(executionContext, functionsLogger, cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    using (var transaction = CreateTransaction())
                    {
                        var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                        var messageContext = CreateMessageContext(message, transportTransaction);

                        await pipeline.PushMessage(messageContext, cancellationToken).ConfigureAwait(false);

                        await messageActions.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }
                catch (Exception exception)
                {
                    using (var transaction = CreateTransaction())
                    {
                        var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                        ErrorContext errorContext = CreateErrorContext(message, transportTransaction, exception);

                        var errorHandleResult = await pipeline.PushFailedMessage(errorContext, cancellationToken).ConfigureAwait(false);

                        if (errorHandleResult == ErrorHandleResult.Handled)
                        {
                            await messageActions.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);

                            transaction.Commit();
                            return;
                        }

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                // abandon message outside of a transaction scope to ensure the abandon operation can't be rolled back
                await messageActions.AbandonMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                throw;
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