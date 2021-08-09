namespace ServiceBus.Tests
{
    using System;
    using NServiceBus;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_connection_string_is_provided
    {
        [Test]
        public void Should_guide_user_towards_success()
        {
            var endpointConfiguration = new EndpointConfiguration("SampleEndpoint");

            endpointConfiguration.UseTransport<ServerlessTransport<AzureServiceBusTransport>>();

            var exception = Assert.ThrowsAsync<Exception>(
                () => Endpoint.Create(endpointConfiguration),
                "Exception should be thrown at endpoint creation so that the error will be found during functions startup"
            );

            StringAssert.Contains(".Transport.ConnectionString(", exception?.Message, "Should mention the transport extension approach");
            StringAssert.Contains("environment variable", exception?.Message, "Should mention the environment variable approach");
        }
    }
}