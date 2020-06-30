namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

            var lockToken = message.SystemProperties.LockToken;
            string messageId;
            Dictionary<string, string> headers;
            byte[] body;

            try
            {
                messageId = message.GetMessageId();
                headers = message.GetHeaders();
                body = message.GetBody();
            }
            catch (Exception exception)
            {
                try
                {
                    await messageReceiver.SafeDeadLetterAsync(transportTransactionMode, lockToken, deadLetterReason: "Poisoned message", deadLetterErrorDescription: exception.Message).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // nothing we can do about it, message will be retried
                }

                return;
            }

            try
            {
                var transportTransaction = CreateTransportTransaction(useTransaction, messageReceiver, message.PartitionKey);
                var messageContext = CreateMessageContext(message, messageId, headers, body, transportTransaction);

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
                        // provide an unmodified copy of the headers
                        var errorContext = CreateErrorContext(exception, messageId, message.GetHeaders(), body, transportTransaction, message.SystemProperties.DeliveryCount);

                        result = await ProcessFailedMessage(errorContext, functionExecutionContext).ConfigureAwait(false);

                        if (result == ErrorHandleResult.Handled)
                        {
                            await messageReceiver.SafeCompleteAsync(transportTransactionMode, lockToken).ConfigureAwait(false);
                        }

                        scope?.Complete();
                    }

                    if (result == ErrorHandleResult.RetryRequired)
                    {
                        await messageReceiver.SafeAbandonAsync(transportTransactionMode, lockToken).ConfigureAwait(false);
                    }
                }
                catch (Exception onErrorException) when (onErrorException is MessageLockLostException || onErrorException is ServiceBusTimeoutException)
                {
                    functionExecutionContext.Logger.LogDebug("Failed to execute recoverability.", onErrorException);
                }
                catch (Exception onErrorException)
                {
                    functionExecutionContext.Logger.LogCritical($"Failed to execute recoverability policy for message with native ID: `{message.MessageId}`", onErrorException);

                    await messageReceiver.SafeAbandonAsync(transportTransactionMode, lockToken).ConfigureAwait(false);
                }
            }
        }

        static MessageContext CreateMessageContext(Message originalMessage, string messageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction)
        {
            var contextBag = new ContextBag();
            contextBag.Set(originalMessage);

            return new MessageContext(
                messageId,
                headers,
                body,
                transportTransaction,
                new CancellationTokenSource(),
                contextBag);
        }

        static ErrorContext CreateErrorContext(Exception exception, string messageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction, int immediateProcessingFailures)
        {
            return new ErrorContext(
                exception,
                headers,
                messageId,
                body,
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
