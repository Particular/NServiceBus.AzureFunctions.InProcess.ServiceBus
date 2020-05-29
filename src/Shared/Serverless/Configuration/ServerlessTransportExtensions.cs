namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Serverless;
    using Transport;

    static class ServerlessTransportExtensions
    {
        public static PipelineInvoker PipelineAccess<TBaseTransport>(
            this TransportExtensions<ServerlessTransport<TBaseTransport>> transportConfiguration) where TBaseTransport : TransportDefinition, new()
        {
            return transportConfiguration.GetSettings().GetOrCreate<PipelineInvoker>();
        }

        public static TransportExtensions<TBaseTransport> BaseTransportConfiguration<TBaseTransport>(
            this TransportExtensions<ServerlessTransport<TBaseTransport>> transportConfiguration) where TBaseTransport : TransportDefinition, new()
        {
            return new TransportExtensions<TBaseTransport>(transportConfiguration.GetSettings());
        }
    }
}