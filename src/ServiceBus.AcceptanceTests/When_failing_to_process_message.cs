namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Pipeline;
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
                CustomizeConfiguration = configuration =>
                {
                    configuration.AdvancedConfiguration.Pipeline.Register(b => new ThrowBeforeCompletingProcessingOfTriggerMessage(), "Simulates failure after dispatch.");
                };
                DoNotFailOnErrorMessages = true;
            }

            public class PublishingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    return context.Publish(new AbortedEvent());
                }
            }

            public class ThrowBeforeCompletingProcessingOfTriggerMessage : Behavior<ITransportReceiveContext>
            {
                public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    await next().ConfigureAwait(false);
                    if (context.Message.Headers[Headers.EnclosedMessageTypes] == typeof(TriggerMessage).FullName)
                    {
                        throw new Exception("Simulated failure after dispatch.");
                    }
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