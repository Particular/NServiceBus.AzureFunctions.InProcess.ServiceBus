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

        [Test]
        public Task DiagnosticIsReportedForMaxAutoLockRenewalDuration()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.MaxAutoLockRenewalDuration|] = new System.TimeSpan(0, 0, 5, 0);

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.MaxAutoLockRenewalDuration|] = new System.TimeSpan(0, 0, 5, 0);
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.MaxAutoLockRenewalDurationNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForPrefetchCount()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.PrefetchCount|] = 5;

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.PrefetchCount|] = 5;
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PrefetchCountNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForPrefetchMultiplier()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.PrefetchMultiplier|] = 5;

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.PrefetchMultiplier|] = 5;
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PrefetchMultiplierNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForTimeToWaitBeforeTriggeringCircuitBreaker()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.TimeToWaitBeforeTriggeringCircuitBreaker|] = new System.TimeSpan(0, 0, 5, 0);

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.TimeToWaitBeforeTriggeringCircuitBreaker|] = new System.TimeSpan(0, 0, 5, 0);
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForPrefetchCountAsExtension()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.PrefetchCount(5)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PrefetchCountNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForPrefetchMultiplierAsExtension()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.PrefetchMultiplier(5)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.PrefetchMultiplierNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForMaxAutoLockRenewalDurationAsExtension()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.MaxAutoLockRenewalDuration(new System.TimeSpan(0, 0, 5, 0))|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.MaxAutoLockRenewalDurationNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForTimeToWaitBeforeTriggeringCircuitBreakerAsExtension()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.TimeToWaitBeforeTriggeringCircuitBreaker(new System.TimeSpan(0, 0, 5, 0))|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId, source);
        }
    }
}