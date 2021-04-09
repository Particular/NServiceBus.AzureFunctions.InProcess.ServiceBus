namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    class NoOpQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            return Task.CompletedTask;
        }
    }
}