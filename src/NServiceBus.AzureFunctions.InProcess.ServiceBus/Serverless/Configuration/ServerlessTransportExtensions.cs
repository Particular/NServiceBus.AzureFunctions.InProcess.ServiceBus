namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using Configuration.AdvancedExtensibility;
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