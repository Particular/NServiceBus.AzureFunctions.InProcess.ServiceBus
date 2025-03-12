namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_message_fails_with_disabled_error_queue
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public void Should_throw_exception(TransportTransactionMode transactionMode)
        {
            var exception = Assert.ThrowsAsync<Exception>(() =>
            {
                return Scenario.Define<ScenarioContext>()
                    .WithComponent(new DisabledErrorQueueFunction(transactionMode))
                    .Done(c => c.EndpointsStarted)
                    .Run();
            });

            Assert.That(exception.Message, Does.Contain("Failed to process message"));
            Assert.That(exception.InnerException, Is.InstanceOf<SimulatedException>());
        }

        class DisabledErrorQueueFunction : FunctionEndpointComponent
        {
            public DisabledErrorQueueFunction(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                CustomizeConfiguration = c => c.DoNotSendMessagesToErrorQueue();

                Messages.Add(new TriggerMessage());
            }

            public class FailingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class TriggerMessage : IMessage;
    }
}