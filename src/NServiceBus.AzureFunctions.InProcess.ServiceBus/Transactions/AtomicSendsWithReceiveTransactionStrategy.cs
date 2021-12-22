namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Transport;

    class AtomicSendsWithReceiveTransactionStrategy : ITransactionStrategy
    {
        readonly ServiceBusReceivedMessage message;
        readonly ServiceBusClient serviceBusClient;
        readonly ServiceBusMessageActions messageActions;

        public AtomicSendsWithReceiveTransactionStrategy(
            ServiceBusReceivedMessage message,
            ServiceBusClient serviceBusClient,
            ServiceBusMessageActions messageActions)
        {
            this.message = message;
            this.serviceBusClient = serviceBusClient;
            this.messageActions = messageActions;
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
            transportTransaction.Set(serviceBusClient);
            transportTransaction.Set("IncomingQueue.PartitionKey", message.PartitionKey);
            transportTransaction.Set(transaction);
            return transportTransaction;
        }

        public async Task Complete(CommittableTransaction transaction, CancellationToken cancellationToken)
        {
            // open short-lived TransactionScope connected to the committable transaction to ensure the message operation has a scope to enlist.
            using (var scope = new TransactionScope(transaction, TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                scope.Complete();
            }
        }
    }
}