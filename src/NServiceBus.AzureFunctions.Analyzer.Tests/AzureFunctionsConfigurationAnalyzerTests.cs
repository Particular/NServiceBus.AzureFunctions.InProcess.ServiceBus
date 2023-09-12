namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using static AzureFunctionsDiagnostics;

    [TestFixture]
    public class AzureFunctionsConfigurationAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)", DefineCriticalErrorActionNotAllowedId)]
        [TestCase("LimitMessageProcessingConcurrencyTo(5)", LimitMessageProcessingToNotAllowedId)]
        [TestCase("MakeInstanceUniquelyAddressable(null)", MakeInstanceUniquelyAddressableNotAllowedId)]
        [TestCase("OverrideLocalAddress(null)", OverrideLocalAddressNotAllowedId)]
        [TestCase("PurgeOnStartup(true)", PurgeOnStartupNotAllowedId)]
        [TestCase("SetDiagnosticsPath(null)", SetDiagnosticsPathNotAllowedId)]
        [TestCase("UseTransport(new AzureServiceBusTransport(null))", UseTransportNotAllowedId)]
        public Task DiagnosticIsReportedForEndpointConfiguration(string configuration, string diagnosticId)
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.{configuration}|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.{configuration}|];
    }}
}}";

            return Assert(diagnosticId, source);
        }
    }
}