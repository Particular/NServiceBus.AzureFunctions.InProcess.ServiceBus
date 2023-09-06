namespace NServiceBus.AzureFunctions.InProcess.ServiceBus.Serverless
{
    class ServerlessInterceptor
    {
        readonly ServerlessTransport transport;

        public ServerlessInterceptor(ServerlessTransport transport) => this.transport = transport;

        public IMessageProcessor MessageProcessor => transport.MessageProcessor;
    }
}
