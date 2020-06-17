namespace NServiceBus.AzureFunctions
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class PipelineInvoker : IPushMessages
    {
        Task IPushMessages.Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            if (this.onMessage == null)
            {
                // The core ReceiveComponent calls TransportInfrastructure.MessagePumpFactory() multiple times
                // the first invocation is for the main pipeline, ignore all other pipelines as we don't want to manually invoke them.
                this.onMessage = onMessage;

                this.onError = onError;
            }

            return Task.CompletedTask;
        }

        void IPushMessages.Start(PushRuntimeSettings limitations)
        {
        }

        Task IPushMessages.Stop()
        {
            return Task.CompletedTask;
        }

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext)
        {
            return onError(errorContext);
        }

        public Task PushMessage(MessageContext messageContext)
        {
            return onMessage.Invoke(messageContext);
        }

        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
    }
}