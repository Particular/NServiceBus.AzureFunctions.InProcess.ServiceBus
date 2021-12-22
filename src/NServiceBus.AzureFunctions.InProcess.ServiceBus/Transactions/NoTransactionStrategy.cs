namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Transport;

    class NoTransactionStrategy : ITransactionStrategy
    {
        public virtual CommittableTransaction CreateTransaction() => null;

        public virtual TransportTransaction CreateTransportTransaction(CommittableTransaction transaction) =>
            new TransportTransaction();

        public virtual Task Complete(CommittableTransaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public static NoTransactionStrategy Instance { get; } = new NoTransactionStrategy();
    }
}