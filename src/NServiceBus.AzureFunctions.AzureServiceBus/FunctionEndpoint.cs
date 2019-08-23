namespace NServiceBus.AzureFunctions.AzureServiceBus
{
    using Microsoft.Azure.WebJobs;
    using Serverless;
    using System;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : ServerlessEndpoint<ExecutionContext>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public FunctionEndpoint(Func<ExecutionContext, ServerlessEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }}
}