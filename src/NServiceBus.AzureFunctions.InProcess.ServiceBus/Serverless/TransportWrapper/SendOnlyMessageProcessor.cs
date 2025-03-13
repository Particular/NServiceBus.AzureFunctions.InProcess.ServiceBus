namespace NServiceBus.AzureFunctions.InProcess.ServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;

class SendOnlyMessageProcessor : IMessageProcessor
{
    public Task ProcessNonAtomic(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'"
        );

    public Task ProcessAtomic(
        ServiceBusReceivedMessage message,
        ServiceBusClient serviceBusClient,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'"
        );
}