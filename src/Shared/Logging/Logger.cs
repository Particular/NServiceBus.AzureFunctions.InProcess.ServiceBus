namespace NServiceBus.AzureFunctions
{
    using System;
    using Logging;
    using Microsoft.Extensions.Logging;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    class Logger : ILog
    {
        public Logger(ILogger logger)
        {
            this.logger = logger;
        }

        public bool IsDebugEnabled => logger.IsEnabled(LogLevel.Debug);
        public bool IsInfoEnabled => logger.IsEnabled(LogLevel.Information);
        public bool IsWarnEnabled => logger.IsEnabled(LogLevel.Warning);
        public bool IsErrorEnabled => logger.IsEnabled(LogLevel.Error);
        public bool IsFatalEnabled => logger.IsEnabled(LogLevel.Critical);

        public void Debug(string message)
        {
            logger.Log(LogLevel.Debug, message);
        }

        public void Debug(string message, Exception exception)
        {
            logger.Log(LogLevel.Debug, exception, message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            logger.Log(LogLevel.Debug, format, args);
        }

        public void Info(string message)
        {
            logger.Log(LogLevel.Information, message);
        }

        public void Info(string message, Exception exception)
        {
            logger.Log(LogLevel.Information, exception, message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            logger.Log(LogLevel.Information, format, args);
        }

        public void Warn(string message)
        {
            logger.Log(LogLevel.Warning, message);
        }

        public void Warn(string message, Exception exception)
        {
            logger.Log(LogLevel.Warning, exception, message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            logger.Log(LogLevel.Warning, format, args);
        }

        public void Error(string message)
        {
            logger.Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            logger.Log(LogLevel.Error, exception, message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            logger.Log(LogLevel.Error, format, args);
        }

        public void Fatal(string message)
        {
            logger.Log(LogLevel.Critical, message);
        }

        public void Fatal(string message, Exception exception)
        {
            logger.Log(LogLevel.Critical, exception, message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            logger.Log(LogLevel.Critical, format, args);
        }

        ILogger logger;
    }
}