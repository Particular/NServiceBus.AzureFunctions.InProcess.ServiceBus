namespace AzureServiceBus.Tests
{
    using NServiceBus.AzureFunctions.ServiceBus;
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void Approve()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(ServiceBusTriggeredEndpointConfiguration).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
            Approver.Verify(publicApi);
        }
    }
}