namespace ServiceBus.Tests
{
    using System;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_connection_string_is_provided
    {
        [Test]
        public void Should_guide_user_towards_success()
        {
            var defaultConnectionStringKey = ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName;
            var connectionString = Environment.GetEnvironmentVariable(defaultConnectionStringKey);

            try
            {
                Environment.SetEnvironmentVariable(defaultConnectionStringKey, null, EnvironmentVariableTarget.Process);

                var exception = Assert.Throws<Exception>(
                    () => new ServiceBusTriggeredEndpointConfiguration("SampleEndpoint", default, true, null),
                    "Exception should be thrown at endpoint creation so that the error will be found during functions startup"
                );

                StringAssert.Contains("UseNServiceBus", exception?.Message, "Should mention the code-first approach");
                StringAssert.Contains("environment variable", exception?.Message, "Should mention the environment variable approach");
            }
            finally
            {
                Environment.SetEnvironmentVariable(defaultConnectionStringKey, connectionString);
            }

        }
    }
}
