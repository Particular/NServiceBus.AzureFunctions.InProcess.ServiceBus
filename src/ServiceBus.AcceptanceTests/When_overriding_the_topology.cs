namespace ServiceBus.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_overriding_the_topology
    {
        [Test]
        public async Task Should_publish_to_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<InsideSubscriber>()
                .WithComponent(new PublishingFunction())
                .Done(c => c.EventReceived)
                .Run();

            Assert.That(context.EventReceived, Is.True);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class InsideSubscriber : EndpointConfigurationBuilder
        {
            public InsideSubscriber() => EndpointSetup<DefaultEndpoint>(_ => { },
                metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(PublishingFunction)));

            class EventHandler(Context testContext) : IHandleMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class PublishingFunction : FunctionEndpointComponent
        {
            public PublishingFunction()
            {
                PublisherMetadata.RegisterPublisherFor<MyEvent>(typeof(PublishingFunction));
                HostConfigurationCustomization = builder =>
                {
                    var customSettings = new Dictionary<string, string>
                    {
                        { "AzureServiceBus:MigrationTopologyOptions:TopicToPublishTo", "bundle-1" },
                        { "AzureServiceBus:MigrationTopologyOptions:TopicToSubscribeOn", "bundle-1" },
                        { $"AzureServiceBus:MigrationTopologyOptions:PublishedEventToTopicsMap:{typeof(MyEvent).FullName}", $"{typeof(MyEvent).ToTopicName()}"
                        },
                    };
                    _ = builder.AddInMemoryCollection(customSettings);
                };
                Messages.Add(new TriggerMessage());
            }

            class PublishingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context) =>
                    context.Publish(new MyEvent());
            }
        }

        class TriggerMessage : IMessage;

        class MyEvent : IEvent;
    }
}