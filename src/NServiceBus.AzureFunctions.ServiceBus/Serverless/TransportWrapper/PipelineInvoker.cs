namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class PipelineInvoker : IMessageReceiver
    {
        public PipelineInvoker(string id) => Id = id;

        public Task Initialize(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            this.onMessage = onMessage;
            this.onError = onError;

            return Task.CompletedTask;
        }

        public Task StartReceive() => Task.CompletedTask;

        public Task StopReceive() => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => null; // TODO can this cause troubles with Autosubscribe?

        public string Id { get; }

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext) => onError(errorContext);

        public Task PushMessage(MessageContext messageContext) => onMessage.Invoke(messageContext);

        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
    }
}