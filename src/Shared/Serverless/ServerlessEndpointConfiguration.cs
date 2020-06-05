namespace NServiceBus.AzureFunctions
{
    using Serialization;
    using Transport;

    /// <summary>
    /// The configuration for an NServiceBus endpoint optimized for serverless environments.
    /// </summary>
    public abstract class ServerlessEndpointConfiguration
    {
        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        protected ServerlessEndpointConfiguration(string endpointName)
        {
            EndpointConfiguration = new EndpointConfiguration(endpointName);

            EndpointConfiguration.UsePersistence<InMemoryPersistence>();

            //make sure a call to "onError" will move the message to the error queue.
            EndpointConfiguration.Recoverability().Delayed(c => c.NumberOfRetries(0));
            // send failed messages to the error queue
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            EndpointConfiguration.Recoverability().CustomPolicy(recoverabilityPolicy.Invoke);
        }

        internal EndpointConfiguration EndpointConfiguration { get; }

        internal PipelineInvoker PipelineInvoker { get; private set; }

        /// <summary>
        /// Gives access to the underlying endpoint configuration for advanced configuration options.
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration => EndpointConfiguration;

        /// <summary>
        /// Define a transport to be used when sending and publishing messages.
        /// </summary>
        protected TransportExtensions<TTransport> UseTransport<TTransport>()
            where TTransport : TransportDefinition, new()
        {
            var serverlessTransport = EndpointConfiguration.UseTransport<ServerlessTransport<TTransport>>();

            PipelineInvoker = serverlessTransport.PipelineAccess();
            return serverlessTransport.BaseTransportConfiguration();
        }

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public SerializationExtensions<T> UseSerialization<T>() where T : SerializationDefinition, new()
        {
            return EndpointConfiguration.UseSerialization<T>();
        }

        /// <summary>
        /// Disables moving messages to the error queue even if an error queue name is configured.
        /// </summary>
        public void DoNotSendMessagesToErrorQueue()
        {
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = false;
        }

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
    }
}
