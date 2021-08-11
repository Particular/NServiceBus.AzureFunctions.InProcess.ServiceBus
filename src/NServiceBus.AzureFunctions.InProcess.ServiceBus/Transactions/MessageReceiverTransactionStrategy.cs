namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using Microsoft.Azure.ServiceBus;
    using Transport;
    using IMessageReceiver = Microsoft.Azure.ServiceBus.Core.IMessageReceiver;

    class MessageReceiverTransactionStrategy : ITransactionStrategy
    {
        readonly Message message;
        readonly IMessageReceiver messageReceiver;

        public MessageReceiverTransactionStrategy(Message message, IMessageReceiver messageReceiver)
        {
            this.message = message;
            this.messageReceiver = messageReceiver;
        }

        public CommittableTransaction CreateTransaction() =>
            new CommittableTransaction(new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TransactionManager.MaximumTimeout
            });

        public TransportTransaction CreateTransportTransaction(CommittableTransaction transaction)
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set((messageReceiver.ServiceBusConnection, messageReceiver.Path));
            transportTransaction.Set("IncomingQueue.PartitionKey", message.PartitionKey);
            transportTransaction.Set(transaction);
            return transportTransaction;
        }

        public async Task Complete(CommittableTransaction transaction)
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