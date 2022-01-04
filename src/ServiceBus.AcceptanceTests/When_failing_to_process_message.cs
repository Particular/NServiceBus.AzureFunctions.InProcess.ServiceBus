namespace ServiceBus.Tests
{
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

            Assert.IsTrue(context.TerminatingEventReceived);
            Assert.IsFalse(context.AbortedEventReceived);
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        public async Task Should_publish_to_subscribers(TransportTransactionMode transactionMode)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<InsideEndpoint>()
                .WithComponent(new PublishingFunction(transactionMode))
                .Done(c => c.TerminatingEventReceived)
                .Run();

            Assert.IsTrue(context.TerminatingEventReceived);
            Assert.IsTrue(context.AbortedEventReceived);
        }

        class Context : ScenarioContext
        {
            public bool AbortedEventReceived { get; set; }
            public bool TerminatingEventReceived { get; set; }
        }

        class InsideEndpoint : EndpointConfigurationBuilder
        {
            public InsideEndpoint()
            {
                EndpointSetup<DefaultEndpoint>(cfg => cfg.LimitMessageProcessingConcurrencyTo(1));
            }

            public class AbortedEventHandler : IHandleMessages<AbortedEvent>
            {
                Context testContext;

                public AbortedEventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(AbortedEvent message, IMessageHandlerContext context)
                {
                    testContext.AbortedEventReceived = true;
                    return Task.CompletedTask;
                }
            }

            public class TerminatingEventHandler : IHandleMessages<TerminatingEvent>
            {
                Context testContext;

                public TerminatingEventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

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
                Messages.Add(new TriggerMessage());
                Messages.Add(new TerminatingMessage());
                DoNotFailOnErrorMessages = true;
                ServiceBusMessageActionsFactory = x => new FirstCompleteFailingServiceBusMessageActions(x);
            }

            public class PublishingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    return context.Publish(new AbortedEvent());
                }
            }

            public class TerminatingMessageHandler : IHandleMessages<TerminatingMessage>
            {
                public Task Handle(TerminatingMessage message, IMessageHandlerContext context)
                {
                    return context.Publish(new TerminatingEvent());
                }
            }
        }

        class FirstCompleteFailingServiceBusMessageActions : ServiceBusMessageActions
        {
            readonly ServiceBusReceiver serviceBusReceiver;
            bool first = true;

            public FirstCompleteFailingServiceBusMessageActions(ServiceBusReceiver serviceBusReceiver)
            {
                this.serviceBusReceiver = serviceBusReceiver;
            }

            public override async Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
            {
                if (first)
                {
                    first = false;
                    await serviceBusReceiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    throw new Exception("Simulated complete failure");
                }

                await serviceBusReceiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }

            public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
            {
                return serviceBusReceiver.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class TerminatingMessage : IMessage
        {
        }

        class AbortedEvent : IEvent
        {
        }

        class TerminatingEvent : IEvent
        {
        }
    }
}