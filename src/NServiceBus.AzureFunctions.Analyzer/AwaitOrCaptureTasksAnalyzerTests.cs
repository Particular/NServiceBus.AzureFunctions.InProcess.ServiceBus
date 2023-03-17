namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AwaitOrCaptureTasksAnalyzerTests : AnalyzerTestFixture<AwaitOrCaptureTasksAnalyzer>
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

            return Assert(AwaitOrCaptureTasksAnalyzer.DiagnosticId, source);
        }
    }
}