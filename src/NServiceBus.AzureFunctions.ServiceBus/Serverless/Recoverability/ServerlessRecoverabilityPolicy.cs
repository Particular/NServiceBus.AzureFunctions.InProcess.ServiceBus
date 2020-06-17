namespace NServiceBus.AzureFunctions
{
    using System;
    using Transport;

    class ServerlessRecoverabilityPolicy
    {
        public bool SendFailedMessagesToErrorQueue { get; set; }

        public RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
        {
            var action = DefaultRecoverabilityPolicy.Invoke(config, errorContext);

            if (action is MoveToError)
            {
                if (SendFailedMessagesToErrorQueue)
                {
                    return action;
                }

                // 7.2 offers a Discard option, but we want to bubble up the exception so it can fail the function invocation.
                throw new Exception("Failed to process message.", errorContext.Exception);
            }

            return action;
        }
    }
}