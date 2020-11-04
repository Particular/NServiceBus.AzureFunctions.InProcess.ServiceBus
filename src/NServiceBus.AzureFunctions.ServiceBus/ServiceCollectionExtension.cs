using System;
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
        /// <param name="serviceCollection">service collection</param>
        /// <param name="configurationFactory">fac</param>
        /// <returns>Return service collection</returns>
        public static IServiceCollection UseNServiceBus(this IServiceCollection serviceCollection,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            var startableEndpoint =
                EndpointWithExternallyManagedServiceProvider.Create(
                    serviceBusTriggeredEndpointConfiguration.EndpointConfiguration, serviceCollection);

            serviceCollection.AddSingleton(sp =>
                new FunctionEndpoint(startableEndpoint, serviceBusTriggeredEndpointConfiguration, sp));

            return serviceCollection;
        }
    }
}