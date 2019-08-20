namespace NServiceBus.Serverless.AzureStorageQueueTrigger
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctionsDemo;
    using Extensibility;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Transport;

    public static class AzureStorageQueueTriggerExtensions
    {
        public static Task Process(this ServerlessEndpoint endpoint, CloudQueueMessage message)
        {
            var serializer = new JsonSerializer();
            var msg = serializer.Deserialize<ASQMessageWrapper>(
                new JsonTextReader(new StreamReader(new MemoryStream(message.AsBytes))));

            var messageContext = new MessageContext(
                Guid.NewGuid().ToString("N"),
                msg.Headers,
                msg.Body,
                new TransportTransaction(),
                new CancellationTokenSource(),
                new ContextBag());

            return endpoint.Process(messageContext);
        }
    }
}