namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using AzureFunctions.InProcess.ServiceBus;
    using Configuration.AdvancedExtensibility;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Azure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Transport.AzureServiceBus;

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
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null) =>
            RegisterEndpointFactory(functionsHostBuilder, null, Assembly.GetCallingAssembly(), (c) => configurationFactory?.Invoke(c));

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            string endpointName,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException($"{nameof(endpointName)} must have a value");
            }
            RegisterEndpointFactory(functionsHostBuilder, endpointName, null, configurationFactory);
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
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException($"{nameof(endpointName)} must have a value");
            }
            RegisterEndpointFactory(functionsHostBuilder, endpointName, null, configurationFactory, connectionString);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var functionsHostBuilderContext = functionsHostBuilder.GetContextInternal();
            var configuration = functionsHostBuilderContext.Configuration;
            var serviceBusTriggeredEndpointConfiguration = configurationFactory(configuration);

            ConfigureEndpointFactory(functionsHostBuilder.Services, functionsHostBuilderContext, serviceBusTriggeredEndpointConfiguration);
        }

        static void RegisterEndpointFactory(IFunctionsHostBuilder functionsHostBuilder,
            string endpointName,
            Assembly callingAssembly,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory,
            string connectionString = null)
        {
            var functionsHostBuilderContext = functionsHostBuilder.GetContextInternal();
            var configuration = functionsHostBuilderContext.Configuration;
            var triggerAttribute = callingAssembly
                    ?.GetCustomAttribute<NServiceBusTriggerFunctionAttribute>();
            var endpointNameValue = triggerAttribute?.EndpointName;
            var connectionName = triggerAttribute?.Connection;

            endpointName ??= configuration?.GetValue<string>("ENDPOINT_NAME") ?? endpointNameValue;

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configuration)` 
- Add a configuration or environment variable with the key ENDPOINT_NAME");
            }

            functionsHostBuilder.Services.AddAzureClientsCore();

            var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, configuration, connectionString, connectionName);

            configurationFactory?.Invoke(functionEndpointConfiguration);

            ConfigureEndpointFactory(functionsHostBuilder.Services, functionsHostBuilderContext, functionEndpointConfiguration);
        }

        static void ConfigureEndpointFactory(IServiceCollection services, FunctionsHostBuilderContext functionsHostBuilderContext,
            ServiceBusTriggeredEndpointConfiguration serviceBusTriggeredEndpointConfiguration)
        {
            var serverless = serviceBusTriggeredEndpointConfiguration.InitializeTransport();
            // When using functions, assemblies are moved to a 'bin' folder within FunctionsHostBuilderContext.ApplicationRootPath.
            var advancedConfiguration = serviceBusTriggeredEndpointConfiguration.AdvancedConfiguration;
            var assemblyDirectoryName = advancedConfiguration.GetSettings().GetOrDefault<string>("NServiceBus.AzureFunctions.InProcess.ServiceBus.AssemblyDirectoryName") ?? "bin";
            var startableEndpoint = Configure(
                advancedConfiguration,
                services,
                Path.Combine(functionsHostBuilderContext.ApplicationRootPath, assemblyDirectoryName));

            _ = services.AddSingleton(serviceBusTriggeredEndpointConfiguration);
            _ = services.AddSingleton(startableEndpoint);
            _ = services.AddSingleton(serverless);
            _ = services.AddSingleton<InProcessFunctionEndpoint>();
            _ = services.AddSingleton<IFunctionEndpoint>(sp => sp.GetRequiredService<InProcessFunctionEndpoint>());

#pragma warning disable CS0618 // Type or member is obsolete
            // Validator is registered here in case the user wants to use the options directly. This makes sure that the options are validated.
            // The transport still has to validate the options because options validators are only executed when the options are resolved.
            _ = services.AddSingleton<IValidateOptions<MigrationTopologyOptions>, MigrationTopologyOptionsValidator>();
            _ = services.AddOptions<MigrationTopologyOptions>()
#pragma warning restore CS0618 // Type or member is obsolete
                .BindConfiguration("AzureServiceBus:MigrationTopologyOptions");

            // Validator is registered here in case the user wants to use the options directly. This makes sure that the options are validated.
            // The transport still has to validate the options because options validators are only executed when the options are resolved.
            _ = services.AddSingleton<IValidateOptions<TopologyOptions>, TopologyOptionsValidator>();
            _ = services.AddOptions<TopologyOptions>().BindConfiguration("AzureServiceBus:TopologyOptions");
        }

        static FunctionsHostBuilderContext GetContextInternal(this IFunctionsHostBuilder functionsHostBuilder)
        {
            // This check is for testing purposes only. See more details on the internal interface below.
            if (functionsHostBuilder is IFunctionsHostBuilderExt internalBuilder)
            {
                return internalBuilder.Context;
            }

            return functionsHostBuilder.GetContext();
        }

        static IStartableEndpointWithExternallyManagedContainer Configure(
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

            return EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);
        }
    }
}