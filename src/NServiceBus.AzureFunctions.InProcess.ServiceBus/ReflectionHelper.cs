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
            var triggerAttribute = FindTriggerAttributeInternal();
            if (triggerAttribute != null)
            {
                return triggerAttribute.AutoComplete;
            }

            throw new Exception($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer the AutoComplete setting. Make sure that the function trigger contains a parameter decorated with {nameof(ServiceBusTriggerAttribute)} or use the advanced APIs exposed via the {nameof(FunctionEndpoint)} type instead.");
        }

        public static ServiceBusTriggerAttribute FindBusTriggerAttribute() => FindTriggerAttributeInternal();

        static ServiceBusTriggerAttribute FindTriggerAttributeInternal()
        {
            var st = new StackTrace(skipFrames: 2); // skip first two frames because it is this method + the public method
            var frames = st.GetFrames();
            foreach (var frame in frames)
            {
                var method = frame?.GetMethod();
                if (method?.GetCustomAttribute<FunctionNameAttribute>(false) != null)
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        ServiceBusTriggerAttribute serviceBusTriggerAttribute;
                        if (parameter.ParameterType == typeof(Message)
                            && (serviceBusTriggerAttribute = parameter.GetCustomAttribute<ServiceBusTriggerAttribute>(false)) != null)
                        {
                            return serviceBusTriggerAttribute;
                        }
                    }

                    return null;
                }
            }

            return null;
        }
    }
}