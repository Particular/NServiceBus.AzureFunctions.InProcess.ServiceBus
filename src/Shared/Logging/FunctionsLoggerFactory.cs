namespace NServiceBus.AzureFunctions
{
    using System;
    using System.Threading;
    using Logging;
    using Microsoft.Extensions.Logging;
    using ILoggerFactory = Logging.ILoggerFactory;

    class FunctionsLoggerFactory : ILoggerFactory
    {
        public static FunctionsLoggerFactory Instance { get; } = new FunctionsLoggerFactory();

        ILog log;

        AsyncLocal<ILogger> logger = new AsyncLocal<ILogger>();

        FunctionsLoggerFactory()
        {
            log = new Logger(logger);
        }

        public void SetCurrentLogger(ILogger currentLogger)
        {
            logger.Value = currentLogger;
        }

        public ILog GetLogger(Type type)
        {
            return log;
        }

        public ILog GetLogger(string name)
        {
            return log;
        }
    }
}