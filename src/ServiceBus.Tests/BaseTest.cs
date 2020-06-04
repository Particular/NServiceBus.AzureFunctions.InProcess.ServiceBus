namespace ServiceBus.Tests
{
    using System.IO;
    using NServiceBus;
    using NUnit.Framework;

    public class BaseTest
    {
        [SetUp]
        public void SetUp()
        {
            // Override Function logic to locate where assemblies are for the testing needs
            FunctionEndpoint.pathFunc = functionExecutionContext => functionExecutionContext.ExecutionContext.FunctionAppDirectory;
        }

        protected Microsoft.Azure.WebJobs.ExecutionContext CreateExecutionContext()
        {
            return new Microsoft.Azure.WebJobs.ExecutionContext
            {
                FunctionAppDirectory = Path.GetDirectoryName(typeof(BaseTest).Assembly.Location)
            };
        }
    }
}