#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Transport;

    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        [ObsoleteEx(ReplacementTypeOrMember = "UseNServiceBus(ENDPOINTNAME, CONNECTIONSTRING)",
                    TreatAsErrorFromVersion = "4",
                    RemoveInVersion = "5")]
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(IConfiguration configuration)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName)
            => throw new NotImplementedException();

        /// <summary>
        /// Define a transport to be used when sending and publishing messages.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        protected TransportExtensions<TTransport> UseTransport<TTransport>()
            where TTransport : TransportDefinition, new()
            => throw new NotImplementedException();
    }

    public static partial class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory) => new NotImplementedException();
    }
}