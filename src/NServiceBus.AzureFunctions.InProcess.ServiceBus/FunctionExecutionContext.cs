namespace NServiceBus
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Contains specific context information of the current function invocation.
    /// </summary>
    public class FunctionExecutionContext
    {
        /// <summary>
        /// Creates a new <see cref="FunctionExecutionContext"/>.
        /// </summary>
        public FunctionExecutionContext(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// The <see cref="ExecutionContext"/> associated with the current function invocation.
        /// </summary>
        public ExecutionContext ExecutionContext => null;

        /// <summary>
        /// The <see cref="ILogger"/> associated with the current function invocation.
        /// </summary>
        public ILogger Logger { get; }
    }
}