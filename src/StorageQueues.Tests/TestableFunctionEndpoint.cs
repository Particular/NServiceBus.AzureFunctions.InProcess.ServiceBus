namespace StorageQueues.Tests
{
    using System;
    using NServiceBus;
    using NUnit.Framework;

    class TestableFunctionEndpoint : FunctionEndpoint
    {
        public TestableFunctionEndpoint(Func<FunctionExecutionContext, StorageQueueTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
            base.AssemblyDirectoryResolver = _ => AppDomain.CurrentDomain.BaseDirectory;

            var value = Environment.GetEnvironmentVariable(StorageQueueTriggeredEndpointConfiguration.DefaultStorageConnectionString);
            Assert.IsNotNull(value, $"Environment variable '{StorageQueueTriggeredEndpointConfiguration.DefaultStorageConnectionString}' should be defined to run tests.");
        }

        public new Func<FunctionExecutionContext, string> AssemblyDirectoryResolver
        {
            set => base.AssemblyDirectoryResolver = value;
        }
    }
}