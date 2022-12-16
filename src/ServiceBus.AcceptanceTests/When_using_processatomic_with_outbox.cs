﻿namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_using_processatomic_with_outbox
    {
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public void Should_dispatch_outgoing_messages_from_the_outbox(TransportTransactionMode transactionMode)
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(() =>
            {
                return Scenario.Define<Context>()
                    .WithComponent(new FunctionHandler(transactionMode))
                    .WithEndpoint<SpyEndpoint>()
                    .Done(c => c.MessageReceived && c.MessageRetried)
                    .Run();
            });

            StringAssert.Contains("Calling ProcessAtomic is not possible when the Outbox is enabled as it would cause a message loss in certain scenarios. Use ProcessNonAtomic for endpoints with Outbox", exception.InnerException.Message);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public bool MessageRetried { get; set; }
        }

        class FunctionHandler : FunctionEndpointComponent
        {
            public FunctionHandler(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                CustomizeConfiguration = configuration =>
                {
                    configuration.AdvancedConfiguration.Pipeline.Register(b => new FailBeforeAckBehavior(b.GetRequiredService<Context>()),
                        "Simulates a failure in ACKing the incoming message");
                    configuration.AdvancedConfiguration.EnableOutbox();
                    configuration.AdvancedConfiguration.UsePersistence<NonDurablePersistence>();
                    //configuration.AdvancedConfiguration.Recoverability().Immediate(x => x.NumberOfRetries(1));
                };
                Messages.Add(new HappyDayMessage());
            }

            public class HappyDayMessageHandler : IHandleMessages<HappyDayMessage>
            {
                public Task Handle(HappyDayMessage message, IMessageHandlerContext context)
                {
                    var sendOptions = new SendOptions();
                    sendOptions.SetDestination(Conventions.EndpointNamingConvention(typeof(SpyEndpoint)));
                    return context.Send(new FollowUpMessage(), sendOptions);
                }
            }
        }

        class SpyEndpoint : EndpointConfigurationBuilder
        {
            public SpyEndpoint()
            {
                EndpointSetup<DefaultEndpoint>();
            }

            public class EventHandler : IHandleMessages<FollowUpMessage>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FollowUpMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class FailBeforeAckBehavior : Behavior<ITransportReceiveContext>
        {
            bool failed;
            Context testContext;

            public FailBeforeAckBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next();

                if (!failed)
                {
                    failed = true;
                    throw new SimulatedException("Simulating ACK failure");
                }
                else
                {
                    testContext.MessageRetried = true;
                }
            }
        }


        class HappyDayMessage : IMessage
        {
        }

        class FollowUpMessage : IMessage
        {
        }
    }
}