using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NServiceBus.AzureFunctions
{
    class Logger : NServiceBus.Logging.ILog
    {
        FunctionsLoggerFactory loggerFactory;
        string loggerName;

        public Logger(FunctionsLoggerFactory loggerFactory, string loggerName)
        {
            this.loggerFactory = loggerFactory;
            this.loggerName = loggerName;
        }

        ILogger log => this.loggerFactory.Logger;

        public bool IsDebugEnabled => log.IsEnabled(LogLevel.Debug);
        public bool IsInfoEnabled => log.IsEnabled(LogLevel.Information);
        public bool IsWarnEnabled => log.IsEnabled(LogLevel.Warning);
        public bool IsErrorEnabled => log.IsEnabled(LogLevel.Error);
        public bool IsFatalEnabled => log.IsEnabled(LogLevel.Error);

        public void Debug(string message)
        {
            log.Log(LogLevel.Debug, message);
        }

        public void Debug(string message, Exception exception)
        {
            log.Log(LogLevel.Debug, exception, message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            log.Log(LogLevel.Debug, format, args);
        }

        public void Info(string message)
        {
            log.Log(LogLevel.Information, message);
        }

        public void Info(string message, Exception exception)
        {
            log.Log(LogLevel.Information, exception, message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            log.Log(LogLevel.Information, format, args);
        }

        public void Warn(string message)
        {
            log.Log(LogLevel.Warning, message);
        }

        public void Warn(string message, Exception exception)
        {
            log.Log(LogLevel.Warning, exception, message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            log.Log(LogLevel.Warning, format, args);
        }

        public void Error(string message)
        {
            log.Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            log.Log(LogLevel.Error, exception, message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            log.Log(LogLevel.Error, format, args);
        }

        public void Fatal(string message)
        {
            log.Log(LogLevel.Error, message);
        }

        public void Fatal(string message, Exception exception)
        {
            log.Log(LogLevel.Error, exception, message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            log.Log(LogLevel.Error, format, args);
        }
    }
}
