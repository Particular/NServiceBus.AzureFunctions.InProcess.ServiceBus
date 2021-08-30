namespace ServiceBus.Tests
{
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    class ServerlessAzureServiceBusTransportTests
    {
        [Test]
        public void Can_change_connection_string_after_construction()
        {
            var transport = new ServerlessAzureServiceBusTransport();
            transport.ChangeConnectionString("NewValue");

            Assert.AreEqual("NewValue", transport.ReadConnectionString(), "ConnectionString was not set.");
        }
    }
}
