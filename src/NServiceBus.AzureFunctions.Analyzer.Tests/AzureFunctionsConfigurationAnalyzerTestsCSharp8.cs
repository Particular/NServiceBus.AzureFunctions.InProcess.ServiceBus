namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;
    using static AzureFunctionsDiagnostics;

    [TestFixture]
    public class AzureFunctionsConfigurationAnalyzerTestsCSharp8 : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        // HINT: In C# 7 this call is ambiguous with the LearningTransport version as the compiler cannot differentiate method calls via generic type constraints
        [TestCase("UseTransport<AzureServiceBusTransport>()", UseTransportNotAllowedId)]
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
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp8;
    }
}