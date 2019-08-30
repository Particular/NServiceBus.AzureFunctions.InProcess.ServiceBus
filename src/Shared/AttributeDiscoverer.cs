namespace NServiceBus.AzureFunctions
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.Azure.WebJobs;

    class TriggerDiscoverer
    {
        /// <summary>
        /// Attempts to derive the required configuration information from the Azure Function and trigger attributes via reflection.
        /// </summary>
        public static TTransportTriggerAttribute TryGet<TTransportTriggerAttribute>() where TTransportTriggerAttribute : Attribute
        {
            var frames = new StackTrace().GetFrames();
            foreach (var stackFrame in frames)
            {
                var method = stackFrame.GetMethod();
                var functionAttribute = method.GetCustomAttribute<FunctionNameAttribute>(false);
                if (functionAttribute != null)
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        var triggerConfiguration = parameter.GetCustomAttribute<TTransportTriggerAttribute>(false);
                        if (triggerConfiguration != null)
                        {
                            return triggerConfiguration;
                        }
                    }
                }
            }

            return null;
        }
    }
}