namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsConfigurationAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        // IEndpointInstance
        [TestCase("ServiceBusTriggeredEndpointConfiguration", "obj.AdvancedConfiguration.PurgeOnStartup(true)")]
        public Task DiagnosticIsReportedForCorePublicMethods(string type, string call)
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar({type} obj)
    {{
        [|{call}|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PurgeOnStartupNotAllowedId, source);
        }
    }
}