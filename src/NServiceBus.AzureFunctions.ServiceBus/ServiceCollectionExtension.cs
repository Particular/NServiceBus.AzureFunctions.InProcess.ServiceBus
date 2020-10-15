using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.AzureFunctions.ServiceBus;

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
        public static IServiceCollection UseNServiceBus(this IServiceCollection serviceCollection, Func<ServerlessEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            var startableEndpoint = EndpointWithExternallyManagedServiceProvider.Create(serviceBusTriggeredEndpointConfiguration.EndpointConfiguration, serviceCollection);

            serviceCollection.Add(new ServiceDescriptor(typeof(PipelineInvoker), provider => serviceBusTriggeredEndpointConfiguration.PipelineInvoker, ServiceLifetime.Singleton));
            serviceCollection.Add(new ServiceDescriptor(typeof(IStartableEndpointWithExternallyManagedContainer), provider => startableEndpoint, ServiceLifetime.Singleton));
            serviceCollection.Add(new ServiceDescriptor(typeof(FunctionEndpoint), typeof(FunctionEndpoint), ServiceLifetime.Singleton));

            return serviceCollection;
        }
    }
}