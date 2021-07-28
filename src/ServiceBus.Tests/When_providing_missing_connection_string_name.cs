namespace ServiceBus.Tests
{
    using System;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_providing_missing_connection_string_name
    {
        [Test]
        public void Should_guide_user_towards_success()
        {
            var exception = Assert.Throws<Exception>(() =>
                    new ServiceBusTriggeredEndpointConfiguration("SampleEndpoint", "DOES_NOT_EXIST"),
                "Exception should be thrown in constructor so that the error will be found during functions startup"
            );

            StringAssert.Contains("environment variable", exception?.Message, "Should mention that there's a missing environment variable");
            StringAssert.Contains("DOES_NOT_EXIST", exception?.Message, "Should mention the specific environment variable");
        }
    }
}