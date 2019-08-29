using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NServiceBus.Logging;

namespace NServiceBus.AzureFunctions
{
    class FunctionsLoggerFactory : NServiceBus.Logging.ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new Logger(this, type.Name);
        }

        public ILog GetLogger(string name)
        {
            return new Logger(this, name);
        }

        public Microsoft.Extensions.Logging.ILogger Logger { get; internal set; }
    }
}
