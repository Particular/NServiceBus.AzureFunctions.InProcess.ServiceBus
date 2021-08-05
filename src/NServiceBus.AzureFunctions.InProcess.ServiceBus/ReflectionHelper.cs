namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;

    class ReflectionHelper
    {
        public static bool GetAutoCompleteValue()
        {
            var st = new StackTrace(skipFrames: 1); // skip first frame because it is this method
            var frames = st.GetFrames();
            foreach (var frame in frames)
            {
                var method = frame?.GetMethod();
                if (method?.GetCustomAttribute<FunctionNameAttribute>() != null)
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        ServiceBusTriggerAttribute serviceBusTriggerAttribute;
                        if (parameter.ParameterType == typeof(Message)
                            && (serviceBusTriggerAttribute = parameter.GetCustomAttribute<ServiceBusTriggerAttribute>()) != null)
                        {
                            return serviceBusTriggerAttribute.AutoComplete;
                        }
                    }
                }
            }

            throw new Exception($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer AutoComplete setting.");
        }
    }
}