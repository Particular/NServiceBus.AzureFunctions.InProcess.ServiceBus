namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus.Core;

    static class MessageReceiverExtensions
    {
        public static Task SafeCompleteAsync(this IMessageReceiver messageReceiver, TransportTransactionMode transportTransactionMode, string lockToken)
        {
            if (transportTransactionMode != TransportTransactionMode.None)
            {
                return messageReceiver.CompleteAsync(lockToken);
            }

            return Task.CompletedTask;
        }

        public static Task SafeAbandonAsync(this IMessageReceiver messageReceiver, TransportTransactionMode transportTransactionMode, string lockToken)
        {
            if (transportTransactionMode != TransportTransactionMode.None)
            {
                return messageReceiver.AbandonAsync(lockToken);
            }

            return Task.CompletedTask;
        }

        public static Task SafeDeadLetterAsync(this IMessageReceiver messageReceiver, TransportTransactionMode transportTransactionMode, string lockToken, string deadLetterReason, string deadLetterErrorDescription)
        {
            if (transportTransactionMode != TransportTransactionMode.None)
            {
                return messageReceiver.DeadLetterAsync(lockToken, deadLetterReason, deadLetterErrorDescription);
            }

            return Task.CompletedTask;
        }
    }
}