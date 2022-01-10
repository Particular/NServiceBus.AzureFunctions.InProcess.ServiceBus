#pragma warning disable 1591

namespace NServiceBus
{
    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        [ObsoleteEx(ReplacementTypeOrMember = "UseNServiceBus(ENDPOINTNAME, CONNECTIONSTRING)",
                    TreatAsErrorFromVersion = "4",
                    RemoveInVersion = "5")]
        public string ServiceBusConnectionString { get; set; }
    }
}