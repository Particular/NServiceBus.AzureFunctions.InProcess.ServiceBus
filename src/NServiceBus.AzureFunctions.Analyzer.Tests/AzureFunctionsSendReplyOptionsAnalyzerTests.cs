namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using static AzureFunctionsDiagnostics;

    [TestFixture]
    public class AzureFunctionsSendReplyOptionsAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("SendOptions", "RouteReplyToAnyInstance", RouteReplyToAnyInstanceNotAllowedId)]
        [TestCase("SendOptions", "RouteReplyToThisInstance", RouteReplyToThisInstanceNotAllowedId)]
        [TestCase("SendOptions", "RouteToThisInstance", RouteToThisInstanceNotAllowedId)]
        [TestCase("ReplyOptions", "RouteReplyToAnyInstance", RouteReplyToAnyInstanceNotAllowedId)]
        [TestCase("ReplyOptions", "RouteReplyToThisInstance", RouteReplyToThisInstanceNotAllowedId)]
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