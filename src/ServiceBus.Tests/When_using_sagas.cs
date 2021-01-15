namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_sagas
    {
        [Test]
        public async Task Should_invoke_saga_message_handlers()
        {
            var context = await Scenario.Define<Context>()
                .WithComponent(new SendingFunction())
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.AreEqual(42, context.CounterValue);
        }

        class Context : ScenarioContext
        {
            public int CounterValue { get; set; }
        }

        class SendingFunction : FunctionEndpointComponent
        {
            public SendingFunction()
            {
                CustomizeConfiguration = configuration =>
                    configuration.AdvancedConfiguration.UsePersistence<LearningPersistence>();

                var correlationProperty = Guid.NewGuid().ToString("N");
                Messages.Add(new StartSagaMessage { CorrelationProperty = correlationProperty });
                Messages.Add(new UpdateSagaMessage { CorrelationProperty = correlationProperty, UpdateValue = 42 });
                Messages.Add(new ReadSagaDataValueMessage { CorrelationProperty = correlationProperty });
            }

            public class DemoSaga : Saga<DemoSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<UpdateSagaMessage>,
                IHandleMessages<ReadSagaDataValueMessage>
            {
                Context testContext;

                public DemoSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DemoSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationProperty);
                    mapper.ConfigureMapping<UpdateSagaMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationProperty);
                    mapper.ConfigureMapping<ReadSagaDataValueMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationProperty);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeCounter = 1;
                    return Task.CompletedTask;
                }

                public Task Handle(UpdateSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeCounter = message.UpdateValue;
                    return Task.CompletedTask;
                }

                public Task Handle(ReadSagaDataValueMessage message, IMessageHandlerContext context)
                {
                    testContext.CounterValue = Data.SomeCounter;
                    MarkAsComplete();
                    return Task.CompletedTask;
                }
            }

            public class DemoSagaData : ContainSagaData
            {
                public string CorrelationProperty { get; set; }
                public int SomeCounter { get; set; }
            }
        }

        class StartSagaMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
        }

        class UpdateSagaMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
            public int UpdateValue { get; set; }
        }

        class ReadSagaDataValueMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
        }
    }
}
