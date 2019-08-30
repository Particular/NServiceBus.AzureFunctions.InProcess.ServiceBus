namespace NServiceBus.AzureFunctions
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public class FunctionExecutionContext
    {
        public FunctionExecutionContext(ExecutionContext context, ILogger logger = null)
        {
            Logger = logger ?? NullLogger.Instance;
            Context = context;
        }

        public ExecutionContext Context { get; }
        public ILogger Logger { get; }
    }
}