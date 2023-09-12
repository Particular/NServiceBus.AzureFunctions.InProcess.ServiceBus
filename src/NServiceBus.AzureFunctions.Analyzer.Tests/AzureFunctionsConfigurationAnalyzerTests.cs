namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsConfigurationAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)", AzureFunctionsDiagnostics.DefineCriticalErrorActionNotAllowedId, LanguageVersion.CSharp7)]
        [TestCase("LimitMessageProcessingConcurrencyTo(5)", AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowedId, LanguageVersion.CSharp7)]
        [TestCase("MakeInstanceUniquelyAddressable(null)", AzureFunctionsDiagnostics.MakeInstanceUniquelyAddressableNotAllowedId, LanguageVersion.CSharp7)]
        [TestCase("OverrideLocalAddress(null)", AzureFunctionsDiagnostics.OverrideLocalAddressNotAllowedId, LanguageVersion.CSharp7)]
        [TestCase("PurgeOnStartup(true)", AzureFunctionsDiagnostics.PurgeOnStartupNotAllowedId, LanguageVersion.CSharp7)]
        [TestCase("SetDiagnosticsPath(null)", AzureFunctionsDiagnostics.SetDiagnosticsPathNotAllowedId, LanguageVersion.CSharp7)]
        // HINT: In C# 7 this call is ambiguous with the LearningTransport version as the compiler cannot differentiate method calls via generic type constraints
        [TestCase("UseTransport<AzureServiceBusTransport>()", AzureFunctionsDiagnostics.UseTransportNotAllowedId, LanguageVersion.CSharp8)]
        [TestCase("UseTransport(new AzureServiceBusTransport(null))", AzureFunctionsDiagnostics.UseTransportNotAllowedId, LanguageVersion.CSharp7)]
        public Task DiagnosticIsReportedForEndpointConfiguration(string configuration, string diagnosticId, LanguageVersion minimumLangVersion)
        {
            testSpecificLangVersion = minimumLangVersion;

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

        LanguageVersion testSpecificLangVersion;
        protected override LanguageVersion AnalyzerLanguageVersion => testSpecificLangVersion;
    }
}