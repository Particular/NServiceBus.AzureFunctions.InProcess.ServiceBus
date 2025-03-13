namespace ServiceBus.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_failing_to_process_message
{
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    public async Task Should_not_publish_to_subscribers(TransportTransactionMode transactionMode)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<InsideEndpoint>()
            .WithComponent(new PublishingFunction(transactionMode))
            .Done(c => c.TerminatingEventReceived)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.TerminatingEventReceived, Is.True);
            Assert.That(context.AbortedEventReceived, Is.False);
        });
    }

    [TestCase(TransportTransactionMode.ReceiveOnly)]
    public async Task Should_publish_to_subscribers(TransportTransactionMode transactionMode)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<InsideEndpoint>()
            .WithComponent(new PublishingFunction(transactionMode))
            .Done(c => c.TerminatingEventReceived)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.TerminatingEventReceived, Is.True);
            Assert.That(context.AbortedEventReceived, Is.True);
        });
    }

    class Context : ScenarioContext
    {
        public bool AbortedEventReceived { get; set; }
        public bool TerminatingEventReceived { get; set; }
        public bool FirstCompleteCalled { get; internal set; }
    }

    class InsideEndpoint : EndpointConfigurationBuilder
    {
        public InsideEndpoint() => EndpointSetup<DefaultEndpoint>(cfg => cfg.LimitMessageProcessingConcurrencyTo(1),
            metadata =>
            {
                metadata.RegisterPublisherFor<AbortedEvent>(typeof(PublishingFunction));
                metadata.RegisterPublisherFor<TerminatingEvent>(typeof(PublishingFunction));
            });

        public class AbortedEventHandler(Context testContext) : IHandleMessages<AbortedEvent>
        {
            public Task Handle(AbortedEvent message, IMessageHandlerContext context)
            {
                testContext.AbortedEventReceived = true;
                return Task.CompletedTask;
            }
        }

        public class TerminatingEventHandler(Context testContext) : IHandleMessages<TerminatingEvent>
        {
            public Task Handle(TerminatingEvent message, IMessageHandlerContext context)
            {
                testContext.TerminatingEventReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    class PublishingFunction : FunctionEndpointComponent
    {
        public PublishingFunction(TransportTransactionMode transportTransactionMode) : base(transportTransactionMode)
        {
            PublisherMetadata.RegisterPublisherFor<AbortedEvent>(typeof(PublishingFunction));
            PublisherMetadata.RegisterPublisherFor<TerminatingEvent>(typeof(PublishingFunction));
            Messages.Add(new TriggerMessage());
            Messages.Add(new TerminatingMessage());
            DoNotFailOnErrorMessages = true;
            ServiceBusMessageActionsFactory = (r, c) => new FirstCompleteFailingServiceBusMessageActions(r, c);
        }

        public class PublishingHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.Publish(new AbortedEvent());
        }

        public class TerminatingMessageHandler : IHandleMessages<TerminatingMessage>
        {
            public Task Handle(TerminatingMessage message, IMessageHandlerContext context) => context.Publish(new TerminatingEvent());
        }
    }

    class FirstCompleteFailingServiceBusMessageActions(
        ServiceBusReceiver serviceBusReceiver,
        ScenarioContext scenarioContext)
        : ServiceBusMessageActions
    {
        readonly Context scenarioContext = (Context)scenarioContext;

        public override async Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            if (!scenarioContext.FirstCompleteCalled)
            {
                scenarioContext.FirstCompleteCalled = true;
                await serviceBusReceiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                throw new Exception("Simulated complete failure");
            }

            await serviceBusReceiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
            => serviceBusReceiver.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
    }

    class TriggerMessage : IMessage;

    class TerminatingMessage : IMessage;

    class AbortedEvent : IEvent;

    class TerminatingEvent : IEvent;
}