namespace ServiceBus.Tests
{
    using System;
    using NServiceBus;

    class TestableFunctionEndpoint : FunctionEndpoint
    {
        public TestableFunctionEndpoint(Func<FunctionExecutionContext, ServiceBusTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
            base.AssemblyDirectoryResolver = _ => AppDomain.CurrentDomain.BaseDirectory;
        }

        public new Func<FunctionExecutionContext, string> AssemblyDirectoryResolver
        {
            set => base.AssemblyDirectoryResolver = value;
        }
    }
}