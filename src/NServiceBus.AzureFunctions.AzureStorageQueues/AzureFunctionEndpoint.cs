namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
    using System;
    using Microsoft.Azure.WebJobs;
    using Serverless;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class AzureFunctionEndpoint : ServerlessEndpoint<ExecutionContext>
    {
        /// <summary>
        /// Create a new endpoint hosting in Azure Function.
        /// </summary>
        public AzureFunctionEndpoint(Func<ExecutionContext, ServerlessEndpointConfiguration> configurationFactory) : base(configurationFactory)
        {
        }}
}