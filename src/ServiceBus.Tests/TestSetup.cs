namespace ServiceBus.Tests
{
    using System;
    using NServiceBus.AcceptanceTesting.Customization;
    using NUnit.Framework;

    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Conventions.EndpointNamingConvention = type =>
            {
                string testName = type.FullName
                    .Replace(type.Namespace ?? string.Empty, string.Empty)
                    .Replace(".When_", string.Empty);
                var parts = testName.Split('+');

                // cap endpoint name at maximum 50 chars length:
                return $"{parts[0].Substring(0, Math.Min(parts[0].Length, 49 - parts[1].Length))}.{parts[1]}";
            };
        }
    }
}