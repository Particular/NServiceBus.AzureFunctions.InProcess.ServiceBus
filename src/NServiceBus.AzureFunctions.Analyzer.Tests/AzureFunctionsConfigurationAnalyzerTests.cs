namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsConfigurationAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [Test]
        public Task DiagnosticIsReportedForPurgeOnStartup()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.PurgeOnStartup(true)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.PurgeOnStartup(true)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PurgeOnStartupNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForLimitMessageProcessingConcurrencyTo()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.LimitMessageProcessingConcurrencyTo(5)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.LimitMessageProcessingConcurrencyTo(5)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowedId, source);
        }

    }
}