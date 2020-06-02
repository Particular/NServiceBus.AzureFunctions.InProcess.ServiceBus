namespace NServiceBus.AzureFunctions
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