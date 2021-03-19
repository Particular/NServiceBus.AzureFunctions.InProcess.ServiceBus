namespace NServiceBus
{
    using System;
    using System.IO;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides extension methods to configure a <see cref="FunctionEndpoint"/> using <see cref="IFunctionsHostBuilder"/>.
    /// </summary>
    public static class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            var endpointFactory = Configure(serviceBusTriggeredEndpointConfiguration, functionsHostBuilder.Services,
                Path.Combine(functionsHostBuilder.GetContext().ApplicationRootPath, "bin"));

            // for backward compatibility
            functionsHostBuilder.Services.AddSingleton(endpointFactory);
            functionsHostBuilder.Services.AddSingleton<IFunctionEndpoint>(sp => sp.GetRequiredService<FunctionEndpoint>());
        }

        internal static Func<IServiceProvider, FunctionEndpoint> Configure(
            ServiceBusTriggeredEndpointConfiguration configuration,
            IServiceCollection serviceCollection,
            string appDirectory)
        {
            configuration.EndpointConfiguration.AssemblyScanner().AdditionalAssemblyScanningPath = appDirectory;

            // Third party assembly brought in by Functions SDK that will appear in both NServiceBus base and additional path for assembly scanning.
            // To avoid exceptions, do not scan it.
            configuration.AdvancedConfiguration.AssemblyScanner().ExcludeAssemblies(FunctionEndpoint.AssembliesToExcludeFromScanning);

            var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
                    configuration.EndpointConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration, serviceProvider);
        }
    }
}