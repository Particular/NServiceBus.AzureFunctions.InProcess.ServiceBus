namespace ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.MessageMutator;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    abstract class SimpleTriggerFunctionComponent : IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor) =>
            Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    CustomizeConfiguration,
                    runDescriptor.ScenarioContext,
                    GetType(),
                    TriggerAction));

        public abstract Task TriggerAction(IFunctionEndpoint endpoint, ExecutionContext executionContext);

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext,
                Type functionComponentType,
                Func<IFunctionEndpoint, ExecutionContext, Task> triggerAction)
            {
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                this.triggerAction = triggerAction;

                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name, default, null);
                var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

                endpointConfiguration.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(d => d.NumberOfRetries(0));

                configurationCustomization(functionEndpointConfiguration);
                var serverless = functionEndpointConfiguration.MakeServerless();

                endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                endpointConfiguration.RegisterComponents(c => c.AddSingleton<IMutateOutgoingTransportMessages>(b => new TestIndependenceMutator(scenarioContext)));

                var serviceCollection = new ServiceCollection();
                var startableEndpointWithExternallyManagedContainer = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = new InProcessFunctionEndpoint(startableEndpointWithExternallyManagedContainer, serverless, serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken)
            {
                await triggerAction(endpoint, new ExecutionContext());
                await base.ComponentsStarted(cancellationToken);
            }

            IFunctionEndpoint endpoint;

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            readonly Func<IFunctionEndpoint, ExecutionContext, Task> triggerAction;
        }
    }
}