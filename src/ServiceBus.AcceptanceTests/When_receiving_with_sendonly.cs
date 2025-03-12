namespace ServiceBus.Tests
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus;
    using NUnit.Framework;
    using System.Threading.Tasks;

    public class When_receiving_with_sendonly
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        public void Should_invoke_the_handler_to_process_it(TransportTransactionMode transactionMode)
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithComponent(new FunctionWithSendOnlyConfiguration(transactionMode))
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain("This endpoint cannot process messages because it is configured in send-only mode."));
        }


        class FunctionWithSendOnlyConfiguration : FunctionEndpointComponent
        {
            public FunctionWithSendOnlyConfiguration(TransportTransactionMode transactionMode) : base(transactionMode)
            {
                CustomizeConfiguration = configuration => configuration.AdvancedConfiguration.SendOnly();

                Messages.Add(new TestMessage());
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Task Handle(TestMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
        }

        class TestMessage : IMessage;
    }
}