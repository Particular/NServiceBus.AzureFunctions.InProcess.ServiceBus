namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;
    using Settings;
    using Transport;

    class ServerlessTransportInfrastructure : TransportInfrastructure
    {
        public ServerlessTransportInfrastructure(TransportInfrastructure baseTransportInfrastructure, SettingsHolder settings)
        {
            this.baseTransportInfrastructure = baseTransportInfrastructure;
            this.settings = settings;
        }

        public override IEnumerable<Type> DeliveryConstraints =>
            baseTransportInfrastructure.DeliveryConstraints;

        //support ReceiveOnly so that we can use immediate retries
        public override TransportTransactionMode TransactionMode { get; } = TransportTransactionMode.ReceiveOnly;

        public override OutboundRoutingPolicy OutboundRoutingPolicy =>
            baseTransportInfrastructure.OutboundRoutingPolicy;

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            var pipelineInvoker = settings.GetOrCreate<PipelineInvoker>();
            return new ManualPipelineInvocationInfrastructure(pipelineInvoker);
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return baseTransportInfrastructure.ConfigureSendInfrastructure();
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return baseTransportInfrastructure.ConfigureSubscriptionInfrastructure();
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return baseTransportInfrastructure.BindToLocalEndpoint(instance);
        }

        public override Task Start()
        {
            return baseTransportInfrastructure.Start();
        }

        public override Task Stop()
        {
            return baseTransportInfrastructure.Stop();
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return baseTransportInfrastructure.ToTransportAddress(logicalAddress);
        }

        readonly TransportInfrastructure baseTransportInfrastructure;
        readonly SettingsHolder settings;
    }
}