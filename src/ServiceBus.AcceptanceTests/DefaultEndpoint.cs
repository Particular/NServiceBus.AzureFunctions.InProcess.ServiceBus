namespace ServiceBus.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;

    class DefaultEndpoint : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(
            RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());
            configuration.EnableInstallers();

            configuration.RegisterComponents(c => c
                .AddSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext));

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            configuration.EnforcePublisherMetadataRegistration(endpointConfiguration.EndpointName, endpointConfiguration.PublisherMetadata);

            var connectionString =
                Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);

            var topology = TopicTopology.Default;
            topology.OverrideSubscriptionNameFor(endpointConfiguration.EndpointName, endpointConfiguration.EndpointName.Shorten());
            foreach (var eventType in endpointConfiguration.PublisherMetadata.Publishers.SelectMany(p => p.Events))
            {
                topology.PublishTo(eventType, eventType.ToTopicName());
                topology.SubscribeTo(eventType, eventType.ToTopicName());
            }
            var azureServiceBusTransport = new AzureServiceBusTransport(connectionString, topology);

            _ = configuration.UseTransport(azureServiceBusTransport);

            configuration.Pipeline.Register("TestIndependenceBehavior", b => new TestIndependenceSkipBehavior(runDescriptor.ScenarioContext), "Skips messages not created during the current test.");

            configuration.UseSerialization<NewtonsoftJsonSerializer>();

            await configurationBuilderCustomization(configuration);

            return configuration;
        }
    }
}