namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    class PipelineInvoker
    {
        internal void Init(OnMessage onMessage, OnError onError)
        {
            this.onMessage = onMessage;
            this.onError = onError;
        }

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext) => onError(errorContext);

        public Task PushMessage(MessageContext messageContext) => onMessage.Invoke(messageContext);

        OnMessage onMessage;
        OnError onError;
    }
}