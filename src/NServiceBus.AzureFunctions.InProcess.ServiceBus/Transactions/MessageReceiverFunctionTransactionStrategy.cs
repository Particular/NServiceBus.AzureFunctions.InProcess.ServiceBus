namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AzureFunctions.InProcess.ServiceBus;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Transport;

    class MessageReceiverFunctionTransactionStrategy : FunctionTransactionStrategy
    {
        readonly Message message;
        readonly IMessageReceiver messageReceiver;

        public MessageReceiverFunctionTransactionStrategy(Message message, IMessageReceiver messageReceiver)
        {
            this.message = message;
            this.messageReceiver = messageReceiver;
        }

        public override CommittableTransaction CreateTransaction() =>
            new CommittableTransaction(new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TransactionManager.MaximumTimeout
            });

        public override TransportTransaction CreateTransportTransaction(CommittableTransaction transaction)
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set((messageReceiver.ServiceBusConnection, messageReceiver.Path));
            transportTransaction.Set("IncomingQueue.PartitionKey", message.PartitionKey);
            transportTransaction.Set(transaction);
            return transportTransaction;
        }

        public override Task Complete(CommittableTransaction transaction) => messageReceiver.SafeCompleteAsync(message, transaction);
    }
}