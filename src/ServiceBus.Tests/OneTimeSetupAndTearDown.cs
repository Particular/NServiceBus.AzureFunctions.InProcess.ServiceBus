namespace ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus.Management;
    using NServiceBus;
    using NUnit.Framework;

    [SetUpFixture]
    public class OneTimeSetupAndTearDown
    {
        [OneTimeSetUp]
        public async Task RunBeforeAllTests()
        {
            var connectionString = Environment.GetEnvironmentVariable(ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName);
            Assert.IsNotNull(connectionString, $"Environment variable '{ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName}' should be defined to run tests.");

            var client = new ManagementClient(connectionString);

            const string errorQueueName = "error";

            if (!await client.QueueExistsAsync(errorQueueName))
            {
                await client.CreateQueueAsync(errorQueueName);
            }

            await client.CloseAsync();
        }
    }
}