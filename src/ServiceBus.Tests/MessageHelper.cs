namespace ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Azure.ServiceBus;
    using NServiceBus;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    public class MessageHelper
    {
        static NewtonsoftJsonSerializer serializer = new NewtonsoftJsonSerializer();
        static IMessageSerializer messageSerializer = serializer.Configure(new SettingsHolder())(new MessageMapper());

        public static Message GenerateMessage(object message)
        {
            Message asbMessage;
            using (var stream = new MemoryStream())
            {
                messageSerializer.Serialize(message, stream);
                asbMessage = new Message(stream.ToArray());
            }

            asbMessage.UserProperties["NServiceBus.EnclosedMessageTypes"] = message.GetType().FullName;

            var systemProperties = new Message.SystemPropertiesCollection();
            // sequence number is required to prevent SystemPropertiesCollection from throwing on the getters
            var fieldInfo = typeof(Message.SystemPropertiesCollection).GetField("sequenceNumber", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(systemProperties, 123);
            // set delivery count to 1
            var deliveryCountProperty = typeof(Message.SystemPropertiesCollection).GetProperty("DeliveryCount");
            deliveryCountProperty.SetValue(systemProperties, 1);
            // assign test message mocked system properties
            var property = typeof(Message).GetProperty("SystemProperties");
            property.SetValue(asbMessage, systemProperties);

            asbMessage.MessageId = Guid.NewGuid().ToString("N");
            return asbMessage;
        }
    }
}