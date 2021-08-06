namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NServiceBus.Logging;
    using NUnit.Framework;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    [TestFixture]
    class LoggingTests
    {
        [Test]
        public void Always_returns_same_logger()
        {
            var loggerA = FunctionsLoggerFactory.Instance.GetLogger("A");
            var loggerB = FunctionsLoggerFactory.Instance.GetLogger("B");

            Assert.AreSame(loggerA, loggerB);
        }

        [Test]
        public void Captures_logs_before_handed_logger()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");
            logger.Info("Deferred message");

            var fakeLogger = new FakeLogger();

            FunctionsLoggerFactory.Instance.SetCurrentLogger(fakeLogger);

            Assert.AreEqual(1, fakeLogger.CapturedLogs.Count);
            fakeLogger.CapturedLogs.TryDequeue(out var capturedLog);
            Assert.AreEqual("Deferred message", capturedLog.message);
        }

        [Test]
        public void Forwards_logs_after_handed_logger()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");

            var fakeLogger = new FakeLogger();

            FunctionsLoggerFactory.Instance.SetCurrentLogger(fakeLogger);

            logger.Info("Forwarded message");

            Assert.AreEqual(1, fakeLogger.CapturedLogs.Count);
            fakeLogger.CapturedLogs.TryDequeue(out var capturedLog);
            Assert.AreEqual("Forwarded message", capturedLog.message);
        }

        [Test]
        public void Only_first_logger_gets_deferred_messages()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");
            logger.Info("Deferred message");

            var firstLogger = new FakeLogger();
            var secondLogger = new FakeLogger();

            FunctionsLoggerFactory.Instance.SetCurrentLogger(firstLogger);
            FunctionsLoggerFactory.Instance.SetCurrentLogger(secondLogger);

            Assert.AreEqual(1, firstLogger.CapturedLogs.Count);
            Assert.AreEqual(0, secondLogger.CapturedLogs.Count);
        }

        [Test]
        public async Task Concurrent_loggers_are_isolated()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");
            var firstLoggerTask = Execute(logger, 1);
            var secondLoggerTask = Execute(logger, 2);

            await Task.WhenAll(firstLoggerTask, secondLoggerTask);

            var firstLogger = firstLoggerTask.Result;
            var secondLogger = secondLoggerTask.Result;

            Assert.AreEqual(1, firstLogger.CapturedLogs.Count);
            Assert.AreEqual(1, secondLogger.CapturedLogs.Count);

            firstLogger.CapturedLogs.TryDequeue(out var firstLog);
            Assert.AreEqual("Running task 1", firstLog.message);

            secondLogger.CapturedLogs.TryDequeue(out var secondLog);
            Assert.AreEqual("Running task 2", secondLog.message);

            async Task<FakeLogger> Execute(ILog log, int n)
            {
                await Task.Yield();
                var fakeLogger = new FakeLogger();
                FunctionsLoggerFactory.Instance.SetCurrentLogger(fakeLogger);
                log.Info($"Running task {n}");
                return fakeLogger;
            }
        }

        class FakeLogger : ILogger
        {
            public ConcurrentQueue<(LogLevel level, EventId eventId, Exception exception, string message)> CapturedLogs
            {
                get;
            } = new ConcurrentQueue<(LogLevel level, EventId eventId, Exception exception, string message)>();


            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
                => CapturedLogs.Enqueue((logLevel, eventId, exception, formatter(state, exception)));

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
        }
    }
}
