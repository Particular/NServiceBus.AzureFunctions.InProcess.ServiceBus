namespace NServiceBus
{
    using System;
    using System.IO;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides extension methods to configure a <see cref="FunctionEndpoint"/> using <see cref="IFunctionsHostBuilder"/>.
    /// </summary>
    public static class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Use the IConfiguration to configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder)
        {
            functionsHostBuilder.UseNServiceBus(config => new ServiceBusTriggeredEndpointConfiguration(config));
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            RegisterEndpointFactory(functionsHostBuilder, serviceBusTriggeredEndpointConfiguration);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var configuration = functionsHostBuilder.GetContext().Configuration;
            var serviceBusTriggeredEndpointConfiguration = configurationFactory(configuration);

            RegisterEndpointFactory(functionsHostBuilder, serviceBusTriggeredEndpointConfiguration);
        }

        static void RegisterEndpointFactory(IFunctionsHostBuilder functionsHostBuilder,
            ServiceBusTriggeredEndpointConfiguration serviceBusTriggeredEndpointConfiguration)
        {
            // When using functions, assemblies are moved to a 'bin' folder within FunctionsHostBuilderContext.ApplicationRootPath.
            var startableEndpoint = Configure(
                serviceBusTriggeredEndpointConfiguration.AdvancedConfiguration,
                functionsHostBuilder.Services,
                Path.Combine(functionsHostBuilder.GetContext().ApplicationRootPath, "bin"));

            functionsHostBuilder.Services.AddSingleton(serviceBusTriggeredEndpointConfiguration);
            functionsHostBuilder.Services.AddSingleton(startableEndpoint);
            functionsHostBuilder.Services.AddSingleton<IFunctionEndpoint, InProcessFunctionEndpoint>();
        }

        internal static IStartableEndpointWithExternallyManagedContainer Configure(
            EndpointConfiguration endpointConfiguration,
            IServiceCollection serviceCollection,
            string appDirectory = null)
        {
            // Load user assemblies from the nested "bin" folder
            InProcessFunctionEndpoint.LoadAssemblies(appDirectory);

            return EndpointWithExternallyManagedServiceProvider.Create(
                    endpointConfiguration,
                    serviceCollection);
        }
    }
}