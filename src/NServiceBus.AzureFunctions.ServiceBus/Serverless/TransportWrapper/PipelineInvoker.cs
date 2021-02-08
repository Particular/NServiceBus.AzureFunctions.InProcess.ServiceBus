namespace NServiceBus.AzureFunctions.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class PipelineInvoker : IMessageReceiver
    {
        readonly IMessageReceiver receiver;

        public PipelineInvoker(IMessageReceiver receiver) => this.receiver = receiver;

        public Task Initialize(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            this.onMessage = onMessage;
            this.onError = onError;

            // initialize the base transport receiver
            return receiver.Initialize(limitations, _ => Task.CompletedTask,
                _ => Task.FromResult(ErrorHandleResult.Handled));

        }

        public Task StartReceive() => Task.CompletedTask; // do not start the base transport receiver

        public Task StopReceive() => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => receiver.Subscriptions;

        public string Id => receiver.Id;

        public Task<ErrorHandleResult> PushFailedMessage(ErrorContext errorContext) => onError(errorContext);

        public Task PushMessage(MessageContext messageContext) => onMessage.Invoke(messageContext);

        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
    }
}