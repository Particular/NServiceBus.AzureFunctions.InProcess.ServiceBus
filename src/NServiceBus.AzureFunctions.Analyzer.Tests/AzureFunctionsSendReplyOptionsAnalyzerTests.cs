namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsSendReplyOptionsAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [Test]
        public Task DiagnosticIsReportedForRouteReplyToThisInstance()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar()
    {{
        var replyOptions = new ReplyOptions();
        [|replyOptions.RouteReplyToThisInstance()|];

        var sendOptions = new SendOptions();
        [|sendOptions.RouteReplyToThisInstance()|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.RouteReplyToThisInstanceNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForRouteToThisInstance()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar()
    {{
        var options = new SendOptions();
        [|options.RouteToThisInstance()|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.RouteToThisInstanceNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForRouteReplyToAnyInstance()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar()
    {{
        var options = new SendOptions();
        [|options.RouteReplyToAnyInstance()|];

        var replyOptions = new ReplyOptions();
        [|replyOptions.RouteReplyToAnyInstance()|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.RouteReplyToAnyInstanceNotAllowedId, source);
        }
    }
}