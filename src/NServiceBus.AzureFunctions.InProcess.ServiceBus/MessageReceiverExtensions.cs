namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    static class MessageReceiverExtensions
    {
        public static async Task SafeCompleteAsync(this IMessageReceiver messageReceiver, Message message, CommittableTransaction transaction)
        {
            // open short-lived TransactionScope connected to the committable transaction to ensure the message operation has a scope to enlist.
            using (var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageReceiver.CompleteAsync(message.SystemProperties.LockToken)
                    .ConfigureAwait(false);
                scope.Complete();
            }
        }
    }
}