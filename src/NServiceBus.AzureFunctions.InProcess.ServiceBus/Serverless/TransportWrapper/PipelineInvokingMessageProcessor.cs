namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus.Serverless;
    using NServiceBus.Extensibility;
    using NServiceBus.Transport.AzureServiceBus;
    using Transport;

    class PipelineInvokingMessageProcessor : IMessageReceiver, IMessageProcessor
    {
        public PipelineInvokingMessageProcessor(IMessageReceiver baseTransportReceiver)
        {
            this.baseTransportReceiver = baseTransportReceiver;
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            return baseTransportReceiver?.Initialize(limitations,
                (_, __) => Task.CompletedTask,
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                cancellationToken) ?? Task.CompletedTask;
        }

        public async Task ProcessNonAtomic(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var messageContext = CreateMessageContext(message, new TransportTransaction(), false);

                await onMessage(messageContext, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception exception)
            {
                var errorContext = CreateErrorContext(message, new TransportTransaction(), exception);

                var errorHandleResult = await onError(errorContext, cancellationToken).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return;
                }
                throw;
            }
        }

        public async Task ProcessAtomic(
            ServiceBusReceivedMessage message,
            ServiceBusClient serviceBusClient,
            ServiceBusMessageActions messageActions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var azureServiceBusTransaction = CreateTransaction(message.PartitionKey, serviceBusClient))
                {
                    var messageContext = CreateMessageContext(message, azureServiceBusTransaction.TransportTransaction, true);

                    await onMessage(messageContext, cancellationToken).ConfigureAwait(false);

                    await SafeCompleteMessageAsync(messageActions, message, azureServiceBusTransaction, cancellationToken).ConfigureAwait(false);
                    azureServiceBusTransaction.Commit();
                }
            }
            catch (Exception exception)
            {
                ErrorHandleResult result;
                using (var azureServiceBusTransaction = CreateTransaction(message.PartitionKey, serviceBusClient))
                {
                    var errorContext = CreateErrorContext(message, azureServiceBusTransaction.TransportTransaction, exception);

                    result = await onError(errorContext, cancellationToken).ConfigureAwait(false);

                    if (result == ErrorHandleResult.Handled)
                    {
                        await SafeCompleteMessageAsync(messageActions, message, azureServiceBusTransaction, cancellationToken).ConfigureAwait(false);
                    }

                    azureServiceBusTransaction.Commit();
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
                ReceiveAddress,
                new ContextBag());
            return errorContext;
        }

        MessageContext CreateMessageContext(ServiceBusReceivedMessage message, TransportTransaction transportTransaction, bool atomic)
        {
            var contextBag = new ContextBag();
            var invocationMode = new FunctionInvocationMode(atomic);
            contextBag.Set(invocationMode);
            var messageContext = new MessageContext(
                message.MessageId,
                message.GetHeaders(),
                message.Body,
                transportTransaction,
                ReceiveAddress,
                contextBag);
            return messageContext;
        }

        static async Task SafeCompleteMessageAsync(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message, AzureServiceBusTransportTransaction azureServiceBusTransaction, CancellationToken cancellationToken = default)
        {
            using var scope = azureServiceBusTransaction.ToTransactionScope();
            await messageActions.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
            scope.Complete();
        }

        static AzureServiceBusTransportTransaction CreateTransaction(string messagePartitionKey, ServiceBusClient serviceBusClient) =>
            new(serviceBusClient, messagePartitionKey, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TransactionManager.MaximumTimeout
            });

        public Task StartReceive(CancellationToken cancellationToken) => Task.CompletedTask;

        // No-op because the rate at which Azure Functions pushes messages to the pipeline can't be controlled.
        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken) => Task.CompletedTask;
        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;

        public string ReceiveAddress => baseTransportReceiver.ReceiveAddress;

        readonly IMessageReceiver baseTransportReceiver;
        OnMessage onMessage;
        OnError onError;
    }
}