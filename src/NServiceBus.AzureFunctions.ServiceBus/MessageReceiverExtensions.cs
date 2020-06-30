﻿namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus.Core;

    internal static class MessageReceiverExtensions
    {
        public static Task SafeCompleteAsync(this MessageReceiver messageReceiver, TransportTransactionMode transportTransactionMode, string lockToken)
        {
            if (transportTransactionMode != TransportTransactionMode.None)
            {
                return messageReceiver.CompleteAsync(lockToken);
            }

            return Task.CompletedTask;
        }

        public static Task SafeAbandonAsync(this MessageReceiver messageReceiver, TransportTransactionMode transportTransactionMode, string lockToken)
        {
            if (transportTransactionMode != TransportTransactionMode.None)
            {
                return messageReceiver.AbandonAsync(lockToken);
            }

            return Task.CompletedTask;
        }
    }
}