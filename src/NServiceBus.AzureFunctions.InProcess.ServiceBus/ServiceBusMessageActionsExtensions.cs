namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;

    static class ServiceBusMessageActionsExtensions
    {
        public static async Task SafeCompleteMessageAsync(this ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message, Transaction committableTransaction, CancellationToken cancellationToken = default)
        {
            using (var scope = new TransactionScope(committableTransaction, TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                scope.Complete();
            }
        }
    }
}