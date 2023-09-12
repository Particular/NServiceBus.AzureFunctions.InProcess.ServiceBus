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

        [Test]
        public Task DiagnosticIsReportedForDefineCriticalErrorAction()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.DefineCriticalErrorActionNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForSetDiagnosticsPath()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.SetDiagnosticsPath(null)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.SetDiagnosticsPath(null)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.SetDiagnosticsPathNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForMakeInstanceUniquelyAddressable()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.MakeInstanceUniquelyAddressable(null)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.MakeInstanceUniquelyAddressable(null)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.MakeInstanceUniquelyAddressableNotAllowedId, source);
        }

        // TODO: Figue out how to test UseTransport<T> extensions
        [Test]
        public Task DiagnosticIsReportedForUseTransport()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.UseTransport(new AzureServiceBusTransport(null))|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.UseTransport(new AzureServiceBusTransport(null))|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.UseTransportNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForOverrideLocalAddress()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.OverrideLocalAddress(null)|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.OverrideLocalAddress(null)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.OverrideLocalAddressNotAllowedId, source);
        }
    }
}