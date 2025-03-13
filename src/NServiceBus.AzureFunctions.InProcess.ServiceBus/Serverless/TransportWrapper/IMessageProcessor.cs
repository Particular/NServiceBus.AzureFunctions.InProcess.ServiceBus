namespace NServiceBus.AzureFunctions.InProcess.ServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;

interface IMessageProcessor
{
    Task ProcessNonAtomic(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = default);

    Task ProcessAtomic(
        ServiceBusReceivedMessage message,
        ServiceBusClient serviceBusClient,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken = default);
}