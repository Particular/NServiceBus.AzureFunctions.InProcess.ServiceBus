namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using AzureFunctions;
    using AzureFunctions.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<ServiceBusTriggeredEndpointConfiguration>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public FunctionEndpoint(Func<FunctionExecutionContext, ServiceBusTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null, MessageReceiver messageReceiver = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            // TODO: get the transaction mode the endpoint is configured with
            var transportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
            var useTransaction = messageReceiver != null && transportTransactionMode == TransportTransactionMode.SendsAtomicWithReceive;

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                var transportTransaction = CreateTransportTransaction(useTransaction, messageReceiver, message.PartitionKey);
                var messageContext = CreateMessageContext(message, transportTransaction);

                using (var scope = useTransaction ? new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled) : null)
                {
                    await Process(messageContext, functionExecutionContext).ConfigureAwait(false);

                    // Azure Functions auto-completion would be disabled if we try to run in SendsAtomicWithReceive, need to complete message manually
                    if (useTransaction)
                    {
                        await messageReceiver.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                    }

                    scope?.Complete();
                }
            }
            catch (Exception exception)
            {
                try
                {
                    ErrorHandleResult result;

                    using (var scope = useTransaction ? new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled) : null)
                    {
                        var transportTransaction = CreateTransportTransaction(useTransaction, messageReceiver, message.PartitionKey);
                        var errorContext = CreateErrorContext(exception, message, transportTransaction, message.SystemProperties.DeliveryCount);

                        result = await ProcessFailedMessage(errorContext, functionExecutionContext).ConfigureAwait(false);

                        if (result == ErrorHandleResult.Handled)
                        {
                            await messageReceiver.SafeCompleteAsync(transportTransactionMode, message.SystemProperties.LockToken).ConfigureAwait(false);
                        }

                        scope?.Complete();
                    }

                    if (result == ErrorHandleResult.RetryRequired)
                    {
                        await messageReceiver.SafeAbandonAsync(transportTransactionMode, message.SystemProperties.LockToken).ConfigureAwait(false);
                    }
                }
                catch (Exception onErrorException) when (onErrorException is MessageLockLostException || onErrorException is ServiceBusTimeoutException)
                {
                    functionExecutionContext.Logger.LogDebug("Failed to execute recoverability.", onErrorException);
                }
                catch (Exception onErrorException)
                {
                    functionExecutionContext.Logger.LogCritical($"Failed to execute recoverability policy for message with native ID: `{message.MessageId}`", onErrorException);

                    await messageReceiver.SafeAbandonAsync(transportTransactionMode, message.SystemProperties.LockToken).ConfigureAwait(false);
                }
            }
        }

        static MessageContext CreateMessageContext(Message originalMessage, TransportTransaction transportTransaction)
        {
            var contextBag = new ContextBag();
            contextBag.Set(originalMessage);

            return new MessageContext(
                originalMessage.GetMessageId(),
                originalMessage.GetHeaders(),
                originalMessage.Body,
                transportTransaction,
                new CancellationTokenSource(),
                contextBag);
        }

        static ErrorContext CreateErrorContext(Exception exception, Message originalMessage, TransportTransaction transportTransaction, int immediateProcessingFailures)
        {
            return new ErrorContext(
                exception,
                originalMessage.GetHeaders(),
                originalMessage.GetMessageId(),
                originalMessage.Body,
                transportTransaction,
                immediateProcessingFailures);
        }

        static TransportTransaction CreateTransportTransaction(bool useTransaction, MessageReceiver messageReceiver, string incomingQueuePartitionKey)
        {
            var transportTransaction = new TransportTransaction();

            if (useTransaction)
            {
                transportTransaction.Set((messageReceiver.ServiceBusConnection, messageReceiver.Path));
                transportTransaction.Set("IncomingQueue.PartitionKey", incomingQueuePartitionKey);
            }

            return transportTransaction;
        }
    }
}
