namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;

    // This is an internal marker interface similar to Microsoft.Azure.Functions.Extensions.DependencyInjection.IFunctionsHostBuilderExt
    // to facilitate test. It will only ever be implemented by the testing infrastructure and probably only required until in process
    // functions properly support the generic host.
    interface IFunctionsHostBuilderExt
    {
        FunctionsHostBuilderContext Context { get; }
    }
}