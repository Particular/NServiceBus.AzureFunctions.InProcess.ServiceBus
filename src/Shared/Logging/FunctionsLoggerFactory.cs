namespace NServiceBus.AzureFunctions
{
    using System;
    using System.Threading;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using ILoggerFactory = Logging.ILoggerFactory;

    class FunctionsLoggerFactory : ILoggerFactory
    {
        public static FunctionsLoggerFactory Instance { get; } = new FunctionsLoggerFactory();

        AsyncLocal<ILogger> logger = new AsyncLocal<ILogger>();

        FunctionsLoggerFactory()
        {
        }

        public void SetCurrentLogger(ILogger currentLogger)
        {
            logger.Value = currentLogger;
        }

        public ILog GetLogger(Type type)
        {
            return new Logger(logger.Value ?? NullLogger.Instance);
        }

        public ILog GetLogger(string name)
        {
            return new Logger(logger.Value ?? NullLogger.Instance);
        }
    }
}