﻿namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Azure.Messaging.ServiceBus;

    static class MessageExtensions
    {
        public static Dictionary<string, string> GetHeaders(this ServiceBusReceivedMessage message)
        {
            var headers = new Dictionary<string, string>(message.ApplicationProperties.Count);

            foreach (var kvp in message.ApplicationProperties)
            {
                headers[kvp.Key] = kvp.Value?.ToString();
            }

            headers.Remove("NServiceBus.Transport.Encoding");

            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                headers[Headers.ReplyToAddress] = message.ReplyTo;
            }

            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                headers[Headers.CorrelationId] = message.CorrelationId;
            }

            return headers;
        }

        public static string GetMessageId(this ServiceBusReceivedMessage message)
        {
            if (string.IsNullOrEmpty(message.MessageId))
            {
                // assume native message w/o message ID
                return Guid.NewGuid().ToString("N");
            }

            return message.MessageId;
        }
    }
}