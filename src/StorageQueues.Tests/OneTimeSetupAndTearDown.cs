namespace StorageQueues.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using NServiceBus;
    using NUnit.Framework;

    [SetUpFixture]
    public class OneTimeSetupAndTearDown
    {
        [OneTimeSetUp]
        public async Task RunBeforeAllTests()
        {
            var connectionString = Environment.GetEnvironmentVariable(StorageQueueTriggeredEndpointConfiguration.DefaultStorageConnectionString);
            Assert.IsNotNull(connectionString, $"Environment variable '{StorageQueueTriggeredEndpointConfiguration.DefaultStorageConnectionString}' should be defined to run tests.");

            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudQueueClient();

            const string errorQueueName = "error";

            var queue = client.GetQueueReference(errorQueueName);

            await queue.CreateIfNotExistsAsync();
        }
    }
}