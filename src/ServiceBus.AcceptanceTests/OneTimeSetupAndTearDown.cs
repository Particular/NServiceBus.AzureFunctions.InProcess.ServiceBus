namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus.Administration;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NUnit.Framework;

    [SetUpFixture]
    public class OneTimeSetupAndTearDown
    {
        [OneTimeSetUp]
        public async Task RunBeforeAllTests()
        {
            var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);
            Assert.IsNotNull(connectionString, $"Environment variable '{ServerlessTransport.DefaultServiceBusConnectionName}' should be defined to run tests.");

            var client = new ServiceBusAdministrationClient(connectionString);

            const string errorQueueName = "error";

            if (!await client.QueueExistsAsync(errorQueueName))
            {
                await client.CreateQueueAsync(errorQueueName);
            }
        }
    }
}