﻿namespace ServiceBus.Tests
{
    using System;
    using Microsoft.Extensions.Configuration;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_providing_connection_string_name_with_missing_value
    {
        [Test]
        public void Should_guide_user_towards_success()
        {
            var exception = Assert.Throws<Exception>(() =>
                {
                    var serviceBusTriggeredEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration("SampleEndpoint", default(IConfiguration));
                    serviceBusTriggeredEndpointConfiguration.ServiceBusConnectionStringName("DOES_NOT_EXIST");
                    serviceBusTriggeredEndpointConfiguration.CreateEndpointConfiguration();
                },
                "Exception should be thrown at endpoint creation so that the error will be found during functions startup"
            );

            StringAssert.Contains("environment value", exception?.Message, "Should mention that there's a missing environment variable");
            StringAssert.Contains("DOES_NOT_EXIST", exception?.Message, "Should mention the specific environment variable");
        }
    }
}