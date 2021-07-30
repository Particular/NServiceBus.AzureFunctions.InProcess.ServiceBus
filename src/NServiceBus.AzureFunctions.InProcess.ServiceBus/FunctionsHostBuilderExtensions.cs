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
            functionsHostBuilder.UseNServiceBus(() => new ServiceBusTriggeredEndpointConfiguration(
                functionsHostBuilder.GetContext().Configuration));
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            // Provides a function to locate the file system directory containing the binaries to be loaded and scanned.
            // When using functions, assemblies are moved to a 'bin' folder within FunctionsHostBuilderContext.ApplicationRootPath.
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
            // Load user assemblies from the nested "bin" folder
            FunctionEndpoint.LoadAssemblies(appDirectory);

            var startableEndpoint = EndpointWithExternallyManagedServiceProvider.Create(
                    configuration.EndpointConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration, serviceProvider);
        }
    }
}