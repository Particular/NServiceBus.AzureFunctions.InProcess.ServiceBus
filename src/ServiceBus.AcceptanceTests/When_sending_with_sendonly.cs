namespace ServiceBus.Tests
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_sending_with_sendonly
    {
        [Test]
        public async Task Should_send_messages()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>()
                .WithComponent(new SendOnlyFunction())
                .Done(c => c.HandlerReceivedMessage)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool HandlerReceivedMessage { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() => EndpointSetup<DefaultEndpoint>();

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                readonly Context testContext;

                public TestMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerReceivedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class SendOnlyFunction : FunctionEndpointComponent
        {
            public SendOnlyFunction() =>
                CustomizeConfiguration = configuration =>
                {
                    configuration.AdvancedConfiguration.SendOnly();

                    configuration.Routing.RouteToEndpoint(typeof(TestMessage), typeof(ReceivingEndpoint));
                };

            protected override Task OnStart(IFunctionEndpoint endpoint, ExecutionContext executionContext)
                => endpoint.Send(new TestMessage(), executionContext);
        }

        class TestMessage : IMessage
        {
        }
    }
}