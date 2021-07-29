namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using Logging;

    class DeferredLoggerFactory : ILoggerFactory
    {
        public static DeferredLoggerFactory Instance { get; } = new DeferredLoggerFactory();

        readonly ConcurrentBag<DeferredLogger> acquiredLoggers = new ConcurrentBag<DeferredLogger>();

        public ILog GetLogger(Type type) => GetLogger(type.FullName);

        public ILog GetLogger(string name)
        {
            var deferredLogger = new DeferredLogger(name);
            acquiredLoggers.Add(deferredLogger);
            return deferredLogger;
        }

        public void FlushAll(ILoggerFactory loggerFactory)
        {
            while (acquiredLoggers.TryTake(out var logger))
            {
                logger.Flush(loggerFactory);
            }
        }
    }
}
