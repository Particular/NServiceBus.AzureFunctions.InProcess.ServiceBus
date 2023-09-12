namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsTransportAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("EntityMaximumSize", "5", AzureFunctionsDiagnostics.EntityMaximumSizeNotAllowedId)]
        [TestCase("MaxAutoLockRenewalDuration", "new System.TimeSpan(0, 0, 5, 0)", AzureFunctionsDiagnostics.MaxAutoLockRenewalDurationNotAllowedId)]
        [TestCase("PrefetchCount", "5", AzureFunctionsDiagnostics.PrefetchCountNotAllowedId)]
        [TestCase("PrefetchMultiplier", "5", AzureFunctionsDiagnostics.PrefetchMultiplierNotAllowedId)]
        [TestCase("TimeToWaitBeforeTriggeringCircuitBreaker", "new System.TimeSpan(0, 0, 5, 0)", AzureFunctionsDiagnostics.TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId)]
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