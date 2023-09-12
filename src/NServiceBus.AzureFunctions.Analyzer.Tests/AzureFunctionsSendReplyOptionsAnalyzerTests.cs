namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsSendReplyOptionsAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("SendOptions", "RouteReplyToAnyInstance", AzureFunctionsDiagnostics.RouteReplyToAnyInstanceNotAllowedId)]
        [TestCase("SendOptions", "RouteReplyToThisInstance", AzureFunctionsDiagnostics.RouteReplyToThisInstanceNotAllowedId)]
        [TestCase("SendOptions", "RouteToThisInstance", AzureFunctionsDiagnostics.RouteToThisInstanceNotAllowedId)]
        [TestCase("ReplyOptions", "RouteReplyToAnyInstance", AzureFunctionsDiagnostics.RouteReplyToAnyInstanceNotAllowedId)]
        [TestCase("ReplyOptions", "RouteReplyToThisInstance", AzureFunctionsDiagnostics.RouteReplyToThisInstanceNotAllowedId)]
        public Task DiagnosticIsReportedForOptions(string optionsType, string method, string diagnosticId)
        {
            var source =
                $@"using NServiceBus; 
class Foo
{{
    void Bar({optionsType} options)
    {{
        [|options.{method}()|];
    }}
}}";

            return Assert(diagnosticId, source);
        }
    }
}