namespace NServiceBus.AzureFunctions
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// </summary>
    public class FunctionExecutionContext
    {
        /// <summary>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="executionContext"></param>
        public FunctionExecutionContext(ExecutionContext executionContext, ILogger logger)
        {
            Logger = logger;
            ExecutionContext = executionContext;
        }

        /// <summary>
        /// </summary>
        public ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// </summary>
        public ILogger Logger { get; }
    }
}