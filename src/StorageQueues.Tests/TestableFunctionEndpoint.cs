namespace StorageQueues.Tests
{
    using System;
    using NServiceBus;

    class TestableFunctionEndpoint : FunctionEndpoint
    {
        public TestableFunctionEndpoint(Func<FunctionExecutionContext, StorageQueueTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
            AssemblyDirectoryResolver = _ => AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}