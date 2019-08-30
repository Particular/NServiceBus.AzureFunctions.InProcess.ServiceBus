namespace NServiceBus.AzureFunctions
{
    using System;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using ILoggerFactory = Logging.ILoggerFactory;

    class FunctionsLoggerFactory : ILoggerFactory
    {
        public FunctionsLoggerFactory(ILogger logger)
        {
            this.logger = logger ?? NullLogger.Instance;
        }

        public ILog GetLogger(Type type)
        {
            return new Logger(logger);
        }

        public ILog GetLogger(string name)
        {
            return new Logger(logger);
        }

        readonly ILogger logger;
    }
}