namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using NServiceBus;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    class ServiceBusTriggeredEndpointConfigurationTests
    {
        [Test]
        public void ConfigurationCallbackMethodOrder()
        {
            var config = new ServiceBusTriggeredEndpointConfiguration("MyEndpoint", default(IConfiguration));
            var callOrder = new List<string>();

            config.Advanced(cfg => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.Advanced)));
            config.Routing(routing => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.Routing)));
            config.ConfigureTransport(transport => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.ConfigureTransport)));
            config.UseSerialization<NewtonsoftSerializer>(serialization => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.UseSerialization)));

            config.CreateEndpointConfiguration();

            Approver.Verify(string.Join(Environment.NewLine, callOrder));
        }
    }
}
