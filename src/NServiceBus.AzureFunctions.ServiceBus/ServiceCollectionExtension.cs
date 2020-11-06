using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace NServiceBus
{
    /// <summary>
    /// Register NServiceBus
    /// </summary>
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Register NServiceBus
        /// </summary>
        public static void UseNServiceBus(this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            FunctionEndpoint.LoadAssemblies(functionsHostBuilder.GetContext().ApplicationRootPath);

            var endpointFactory = Configure(serviceBusTriggeredEndpointConfiguration, functionsHostBuilder.Services,
                functionsHostBuilder.GetContext().ApplicationRootPath);

            functionsHostBuilder.Services.AddSingleton(endpointFactory);
        }

        internal static Func<IServiceProvider, FunctionEndpoint> Configure(ServiceBusTriggeredEndpointConfiguration configuration, IServiceCollection serviceCollection, string appDirectory)
        {
            FunctionEndpoint.LoadAssemblies(appDirectory);

            var startableEndpoint =
                EndpointWithExternallyManagedServiceProvider.Create(
                    configuration.EndpointConfiguration, serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration,
                serviceProvider);
        }
    }
}