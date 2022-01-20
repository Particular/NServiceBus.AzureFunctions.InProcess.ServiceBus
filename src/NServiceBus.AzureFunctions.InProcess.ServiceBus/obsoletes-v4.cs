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

        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(IConfiguration configuration)
            => throw new NotImplementedException();

        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null)
            => throw new NotImplementedException();

        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null)
            => throw new NotImplementedException();

        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName)
            => throw new NotImplementedException();

        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        protected TransportExtensions<TTransport> UseTransport<TTransport>()
            where TTransport : TransportDefinition, new()
            => throw new NotImplementedException();
    }

    public static partial class FunctionsHostBuilderExtensions
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory) => new NotImplementedException();
    }
}

#pragma warning restore 1591