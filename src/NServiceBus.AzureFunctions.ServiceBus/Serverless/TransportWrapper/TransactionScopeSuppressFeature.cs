namespace NServiceBus.AzureFunctions.ServiceBus
{
    using Features;

    class TransactionScopeSuppressFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new TransactionScopeSuppressBehavior.Registration());
        }
    }
}