namespace ServiceBus.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.WebJobs.ServiceBus;

    class TestableServiceBusMessageActions : ServiceBusMessageActions
    {
        readonly ServiceBusReceiver serviceBusReceiver;

        public TestableServiceBusMessageActions(ServiceBusReceiver serviceBusReceiver)
        {
            this.serviceBusReceiver = serviceBusReceiver;
        }

        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            return serviceBusReceiver.CompleteMessageAsync(message, cancellationToken);
        }

        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            return serviceBusReceiver.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
        }
    }
}