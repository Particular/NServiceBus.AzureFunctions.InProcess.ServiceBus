namespace ServiceBus.Tests
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_connection_string_is_provided
    {
        [SetUp]
        public void SetUp()
        {
            var defaultConnectionStringKey = ServerlessTransport.DefaultServiceBusConnectionName;
            originalConnectionString = Environment.GetEnvironmentVariable(defaultConnectionStringKey);

            Environment.SetEnvironmentVariable(defaultConnectionStringKey, null, EnvironmentVariableTarget.Process);
        }

        [Test]
        public void Should_guide_user_towards_success()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
                    .WithComponent(new FunctionWithoutConnectionString())
                    .Done(c => c.EndpointsStarted)
                    .Run(),
                "Exception should be thrown at endpoint creation so that the error will be found during functions startup"
            );

            StringAssert.Contains("UseNServiceBus", exception?.Message, "Should mention the code-first approach");
            StringAssert.Contains("environment variable", exception?.Message,
                "Should mention the environment variable approach");
        }

        [TearDown]
        public void TearDown() =>
            Environment.SetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName,
                originalConnectionString);

        string originalConnectionString;

        class FunctionWithoutConnectionString : FunctionEndpointComponent
        {
        }
    }
}