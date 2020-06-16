namespace ServiceBus.Tests
{
    using System;
    using NServiceBus;
    using NUnit.Framework;

    class TestableFunctionEndpoint : FunctionEndpoint
    {
        public TestableFunctionEndpoint(Func<FunctionExecutionContext, ServiceBusTriggeredEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
            base.AssemblyDirectoryResolver = _ => AppDomain.CurrentDomain.BaseDirectory;

            var value = Environment.GetEnvironmentVariable(ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName);
            Assert.IsNotNull(value, $"Environment variable '{ServiceBusTriggeredEndpointConfiguration.DefaultServiceBusConnectionName}' should be defined to run tests.");
        }

        public new Func<FunctionExecutionContext, string> AssemblyDirectoryResolver
        {
            set => base.AssemblyDirectoryResolver = value;
        }
    }
}