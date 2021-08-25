namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides extension methods to configure a <see cref="FunctionEndpoint"/> using <see cref="IFunctionsHostBuilder"/>.
    /// </summary>
    public static partial class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            var hostConfiguration = functionsHostBuilder.GetContext().Configuration;
            var endpointName = hostConfiguration.GetValue<string>("ENDPOINT_NAME")
                ?? Assembly.GetCallingAssembly()
                    .GetCustomAttribute<NServiceBusTriggerFunctionAttribute>()
                    ?.EndpointName;

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configurationFactory)` 
- Add a configuration or environment variable with the key ENDPOINT_NAME");
            }

            functionsHostBuilder.UseNServiceBus(endpointName, configurationFactory);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            string endpointName,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            var config = functionsHostBuilder.GetContext().Configuration;
            var serviceBusConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, config);
            configurationFactory?.Invoke(serviceBusConfiguration);
            RegisterEndpointFactory(functionsHostBuilder, serviceBusConfiguration);
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
            var endpointConfiguration = configuration.AdvancedConfiguration;

            var scanner = endpointConfiguration.AssemblyScanner();
            scanner.AdditionalAssemblyScanningPath = appDirectory;
            scanner.ExcludeAssemblies(FunctionEndpoint.AssembliesToExcludeFromScanning);

            if (string.IsNullOrWhiteSpace(configuration.ServiceBusConnectionString))
            {
                throw new Exception($@"Azure Service Bus connection string has not been configured. Specify a connection string through IConfiguration, an environment variable named {ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName} or using:
            `serviceBusTriggeredEndpointConfiguration.{nameof(ServiceBusTriggeredEndpointConfiguration.ServiceBusConnectionString)}");
            }

            var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
                    endpointConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration, serviceProvider);
        }
    }
}