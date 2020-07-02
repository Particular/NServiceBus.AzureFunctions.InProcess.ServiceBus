namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System;
    using System.Threading;
    using Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    class Logger : ILog
    {
        public Logger(AsyncLocal<ILogger> logger)
        {
            this.logger = logger;
        }

        ILogger CurrentLogger => logger.Value ?? NullLogger.Instance;

        public bool IsDebugEnabled => CurrentLogger.IsEnabled(LogLevel.Debug);
        public bool IsInfoEnabled => CurrentLogger.IsEnabled(LogLevel.Information);
        public bool IsWarnEnabled => CurrentLogger.IsEnabled(LogLevel.Warning);
        public bool IsErrorEnabled => CurrentLogger.IsEnabled(LogLevel.Error);
        public bool IsFatalEnabled => CurrentLogger.IsEnabled(LogLevel.Critical);

        public void Debug(string message)
        {
            CurrentLogger.Log(LogLevel.Debug, message);
        }

        public void Debug(string message, Exception exception)
        {
            CurrentLogger.Log(LogLevel.Debug, exception, message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            CurrentLogger.Log(LogLevel.Debug, format, args);
        }

        public void Info(string message)
        {
            CurrentLogger.Log(LogLevel.Information, message);
        }

        public void Info(string message, Exception exception)
        {
            CurrentLogger.Log(LogLevel.Information, exception, message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            CurrentLogger.Log(LogLevel.Information, format, args);
        }

        public void Warn(string message)
        {
            CurrentLogger.Log(LogLevel.Warning, message);
        }

        public void Warn(string message, Exception exception)
        {
            CurrentLogger.Log(LogLevel.Warning, exception, message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            CurrentLogger.Log(LogLevel.Warning, format, args);
        }

        public void Error(string message)
        {
            CurrentLogger.Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            CurrentLogger.Log(LogLevel.Error, exception, message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            CurrentLogger.Log(LogLevel.Error, format, args);
        }

        public void Fatal(string message)
        {
            CurrentLogger.Log(LogLevel.Critical, message);
        }

        public void Fatal(string message, Exception exception)
        {
            CurrentLogger.Log(LogLevel.Critical, exception, message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            CurrentLogger.Log(LogLevel.Critical, format, args);
        }

        AsyncLocal<ILogger> logger;
    }
}