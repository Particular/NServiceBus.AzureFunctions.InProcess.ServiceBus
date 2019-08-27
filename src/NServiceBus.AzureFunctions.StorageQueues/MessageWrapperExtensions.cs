namespace NServiceBus.AzureFunctions.StorageQueues
{
    using System;
    using System.Collections.Generic;
    using Azure.Transports.WindowsAzureStorageQueues;

    static class MessageWrapperExtensions
    {
        public static Dictionary<string, string> GetHeaders(this MessageWrapper message)
        {
            return new Dictionary<string, string>(message.Headers);
        }

        public static string GetMessageId(this MessageWrapper message)
        {
            if (string.IsNullOrEmpty(message.Id))
            {
                // assume native message w/o message ID
                return Guid.NewGuid().ToString("N");
            }

            return message.Id;
        }
    }
}