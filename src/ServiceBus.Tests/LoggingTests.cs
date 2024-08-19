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

            Assert.That(loggerB, Is.SameAs(loggerA));
        }

        [Test]
        public void Captures_logs_before_handed_logger()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");
            logger.Info("Deferred message");

            var fakeLogger = new FakeLogger();

            FunctionsLoggerFactory.Instance.SetCurrentLogger(fakeLogger);

            Assert.That(fakeLogger.CapturedLogs.Count, Is.EqualTo(1));
            fakeLogger.CapturedLogs.TryDequeue(out var capturedLog);
            Assert.That(capturedLog.message, Is.EqualTo("Deferred message"));
        }

        [Test]
        public void Forwards_logs_after_handed_logger()
        {
            var logger = FunctionsLoggerFactory.Instance.GetLogger("logger");

            var fakeLogger = new FakeLogger();

            FunctionsLoggerFactory.Instance.SetCurrentLogger(fakeLogger);

            logger.Info("Forwarded message");

            Assert.That(fakeLogger.CapturedLogs.Count, Is.EqualTo(1));
            fakeLogger.CapturedLogs.TryDequeue(out var capturedLog);
            Assert.That(capturedLog.message, Is.EqualTo("Forwarded message"));
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

            Assert.That(firstLogger.CapturedLogs.Count, Is.EqualTo(1));
            Assert.That(secondLogger.CapturedLogs.Count, Is.EqualTo(0));
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

            Assert.That(firstLogger.CapturedLogs.Count, Is.EqualTo(1));
            Assert.That(secondLogger.CapturedLogs.Count, Is.EqualTo(1));

            firstLogger.CapturedLogs.TryDequeue(out var firstLog);
            Assert.That(firstLog.message, Is.EqualTo("Running task 1"));

            secondLogger.CapturedLogs.TryDequeue(out var secondLog);
            Assert.That(secondLog.message, Is.EqualTo("Running task 2"));

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
