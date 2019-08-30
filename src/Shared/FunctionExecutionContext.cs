namespace NServiceBus.AzureFunctions
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Function execution context
    /// </summary>
    public class FunctionExecutionContext
    {
        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public FunctionExecutionContext(ExecutionContext context, ILogger logger = null)
        {
            Logger = logger ?? NullLogger.Instance;
            Context = context;
        }

        /// <summary>
        /// </summary>
        public ExecutionContext Context { get; }
        
        /// <summary>
        /// </summary>
        public ILogger Logger { get; }
    }
}