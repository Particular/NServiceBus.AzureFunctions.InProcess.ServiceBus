namespace StorageQueues.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Features;

    class DefaultEndpoint : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(
            RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());
            configuration.EnableInstallers();

            configuration.DisableFeature<TimeoutManager>();
            configuration.UsePersistence<InMemoryPersistence>();

            configuration.RegisterComponents(c => c
                .RegisterSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext));

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            var transport = configuration.UseTransport<AzureStorageQueueTransport>();
            transport.ConnectionString(Environment.GetEnvironmentVariable(StorageQueueTriggeredEndpointConfiguration.DefaultStorageConnectionString));

            configuration.UseSerialization<NewtonsoftSerializer>();

            configurationBuilderCustomization(configuration);

            return Task.FromResult(configuration);
        }

        
    }
}