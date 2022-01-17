namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides extension methods to configure a <see cref="IFunctionEndpoint"/> using <see cref="IFunctionsHostBuilder"/>.
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
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configuration)` 
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

            var serviceBusConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, config, GetTransportTransactionMode());
            configurationFactory?.Invoke(serviceBusConfiguration);
            RegisterEndpointFactory(functionsHostBuilder, serviceBusConfiguration);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            string endpointName,
            string connectionString,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            var config = functionsHostBuilder.GetContext().Configuration;
            var serviceBusConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, config, GetTransportTransactionMode(), connectionString);
            configurationFactory?.Invoke(serviceBusConfiguration);
            RegisterEndpointFactory(functionsHostBuilder, serviceBusConfiguration);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
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
            var scanner = endpointConfiguration.AssemblyScanner();
            if (appDirectory != null)
            {
                scanner.AdditionalAssemblyScanningPath = appDirectory;
            }

            scanner.ExcludeAssemblies(InProcessFunctionEndpoint.AssembliesToExcludeFromScanning);

            return EndpointWithExternallyManagedContainer.Create(
                    endpointConfiguration,
                    serviceCollection);
        }

        static TransportTransactionMode GetTransportTransactionMode()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var attribute = assembly.GetCustomAttribute<NServiceBusTriggerFunctionAttribute>();

                if (attribute != null)
                {
                    return attribute.SendsAtomicWithReceive ? TransportTransactionMode.SendsAtomicWithReceive : TransportTransactionMode.ReceiveOnly;
                }
            }
            return TransportTransactionMode.SendsAtomicWithReceive;
        }
    }
}