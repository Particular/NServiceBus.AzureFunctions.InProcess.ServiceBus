namespace NServiceBus.Serverless
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Settings;
    using Transport;

    class DummyDispatcher : TransportDefinition
    {
        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new DummyTransportInfrastructure();
        }

        public class DummyTransportInfrastructure : TransportInfrastructure
        {
            public override IEnumerable<Type> DeliveryConstraints => Enumerable.Empty<Type>();
            public override TransportTransactionMode TransactionMode => TransportTransactionMode.None;
            public override OutboundRoutingPolicy OutboundRoutingPolicy => new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
            public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure() => new DummyReceiver();

            public override TransportSendInfrastructure ConfigureSendInfrastructure() => new DummySender();

            public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
            {
                return instance;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.ToString();
            }

            public class DummyReceiver : TransportReceiveInfrastructure
            {
                public DummyReceiver() : base(
                    () => new DummyInfrastructureImpl(),
                    () => new DummyInfrastructureImpl(),
                    () => Task.FromResult(StartupCheckResult.Success))
                {
                }

                class DummyInfrastructureImpl : IPushMessages, ICreateQueues
                {
                    public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
                    {
                        return Task.CompletedTask;
                    }

                    public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
                    {
                        return Task.CompletedTask;
                    }

                    public void Start(PushRuntimeSettings limitations)
                    {
                    }

                    public Task Stop()
                    {
                        return Task.CompletedTask;
                    }
                }
            }

            public class DummySender : TransportSendInfrastructure
            {
                public DummySender() : base(
                    () => new DummyDispatcher(),
                    () => Task.FromResult(StartupCheckResult.Success))
                {
                }

                class DummyDispatcher : IDispatchMessages
                {
                    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}