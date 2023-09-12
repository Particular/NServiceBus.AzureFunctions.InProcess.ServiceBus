namespace NServiceBus.AzureFunctions.Analyzer.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class AzureFunctionsTransportAnalyzerTests : AnalyzerTestFixture<AzureFunctionsConfigurationAnalyzer>
    {
        [TestCase("PrefetchCount", "5", AzureFunctionsDiagnostics.PrefetchCountNotAllowedId)]
        public Task DiagnosticIsReportedTransportConfiguration(string configName, string configValue, string diagnosticId)
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Direct(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.{configName}|] = {configValue};

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.{configName}|] = {configValue};
    }}

    void Extension(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.{configName}({configValue})|];
    }}
}}";

            return Assert(diagnosticId, source);
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

        [Test]
        public Task DiagnosticIsReportedForEntityMaximumSize()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.EntityMaximumSize|] = 5;

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.EntityMaximumSize|] = 5;
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.EntityMaximumSizeNotAllowedId, source);
        }

        [Test]
        public Task DiagnosticIsReportedForEntityMaximumSizeAsExtension()
        {
            var source =
                $@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.EntityMaximumSize(1)|];
    }}
}}";

            return Assert(AzureFunctionsDiagnostics.EntityMaximumSizeNotAllowedId, source);
        }
    }
}