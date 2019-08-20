namespace NServiceBus.Serverless.AzureServiceBusTrigger
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Transport;

    public static class AzureServiceBusTriggerExtensions
    {
        public static Task Process(this ServerlessEndpoint endpoint, Message message)
        {
            var context = new MessageContext(
                Guid.NewGuid().ToString("N"),
                message.UserProperties.ToDictionary(x => x.Key, x => x.Value.ToString()),
                message.Body,
                new TransportTransaction(),
                new CancellationTokenSource(),
                new ContextBag());

            return endpoint.Process(context);
        }
    }
}