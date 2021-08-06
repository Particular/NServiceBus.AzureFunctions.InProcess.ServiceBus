namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using Transport;

    interface ITransactionStrategy
    {
        CommittableTransaction CreateTransaction();
        TransportTransaction CreateTransportTransaction(CommittableTransaction transaction);
        Task Complete(CommittableTransaction transaction);
    }
}