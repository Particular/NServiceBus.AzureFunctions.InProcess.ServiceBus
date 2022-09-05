namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;

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

            var connectionString =
                Environment.GetEnvironmentVariable(ServiceBusTriggeredEndpointConfiguration
                    .DefaultServiceBusConnectionName);

            var azureServiceBusTransport = new AzureServiceBusTransport(connectionString)
            {
                SubscriptionRuleNamingConvention = type =>
                {
                    if (type.FullName.Length <= 50)
                    {
                        return type.FullName;
                    }

                    return type.Name;
                }
            };

            var transport = configuration.UseTransport(azureServiceBusTransport);

            configuration.Pipeline.Register("TestIndependenceBehavior", b => new TestIndependenceSkipBehavior(runDescriptor.ScenarioContext), "Skips messages not created during the current test.");

            configuration.UseSerialization<NewtonsoftJsonSerializer>();

            await configurationBuilderCustomization(configuration);

            return configuration;
        }
    }
}