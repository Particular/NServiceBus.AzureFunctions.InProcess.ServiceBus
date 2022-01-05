namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using Azure.Messaging.ServiceBus;

    public class MessageHelper
    {
        public static ServiceBusReceivedMessage GenerateMessage(object message)
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromObjectAsJson(message),
                messageId: Guid.NewGuid().ToString("N"),
                deliveryCount: 1,
                properties: new Dictionary<string, object> { { "NServiceBus.EnclosedMessageTypes", message.GetType().FullName } });
        }
    }
}