namespace ServiceBus.Tests
{
    using System.Reflection;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    class AzureServiceBusTransportExtensionsTests
    {
        [Test]
        public void Can_change_connection_string()
        {
            var property = typeof(AzureServiceBusTransport).GetProperty("ConnectionString",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(property, "AzureServiceBusTransport.ConnectionString not found");

            var transport = new AzureServiceBusTransport("OldValue");
            transport.ChangeConnectionString("NewValue");

            var recoveredValue = (string)property.GetValue(transport);
            Assert.AreEqual("NewValue", recoveredValue, "ConnectionString was not set.");
        }
    }
}
