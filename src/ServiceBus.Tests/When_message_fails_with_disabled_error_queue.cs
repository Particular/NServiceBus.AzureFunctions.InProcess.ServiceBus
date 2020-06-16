namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_message_fails_with_disabled_error_queue
    {
        [Test]
        public void Should_throw_exception()
        {
            var exception = Assert.ThrowsAsync<Exception>(() =>
            {
                return Scenario.Define<ScenarioContext>()
                    .WithComponent(new FailingFunction(new TriggerMessage()))
                    .Done(c => c.EndpointsStarted)
                    .Run();
            });

            StringAssert.Contains("Failed to process message", exception.Message);
            Assert.IsInstanceOf<SimulatedException>(exception.InnerException);
        }

        class FailingFunction : FunctionEndpointComponent
        {
            public FailingFunction(object triggerMessage) : base(triggerMessage, c =>
            {
                c.DoNotSendMessagesToErrorQueue();
            }) { }

            public class FailingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class TriggerMessage : IMessage
        {
        }
    }
}