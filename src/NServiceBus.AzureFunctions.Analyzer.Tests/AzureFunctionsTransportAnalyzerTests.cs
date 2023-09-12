namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using static AzureFunctionsDiagnostics;

    [TestFixture]
    public class AzureFunctionsTransportAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("EntityMaximumSize", "5", EntityMaximumSizeNotAllowedId)]
        [TestCase("MaxAutoLockRenewalDuration", "new System.TimeSpan(0, 0, 5, 0)", MaxAutoLockRenewalDurationNotAllowedId)]
        [TestCase("PrefetchCount", "5", PrefetchCountNotAllowedId)]
        [TestCase("PrefetchMultiplier", "5", PrefetchMultiplierNotAllowedId)]
        [TestCase("TimeToWaitBeforeTriggeringCircuitBreaker", "new System.TimeSpan(0, 0, 5, 0)", TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId)]
        public Task DiagnosticIsReportedTransportConfiguration(string configName, string configValue, string diagnosticId)
        {
            var source =
                $@"using NServiceBus;
using System;
using System.Threading.Tasks;
class Foo
{{
    void Direct(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.{configName}|] = {configValue};

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.{configName}|] = {configValue};
    }}

    void Extension(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.{configName}({configValue})|];
    }}
}}";

            return Assert(diagnosticId, source);
        }
    }
}