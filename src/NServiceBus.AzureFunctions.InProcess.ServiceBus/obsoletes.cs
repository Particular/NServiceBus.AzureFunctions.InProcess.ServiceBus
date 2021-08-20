#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    public static partial class FunctionsHostBuilderExtensions
    {
        [ObsoleteEx(ReplacementTypeOrMember = "UseNServiceBus(string, Action<ServiceBusTriggeredEndpointConfiguration>)",
            TreatAsErrorFromVersion = "2",
            RemoveInVersion = "3")]
        public static void UseNServiceBus(
            this IFunctionsHostBuilder functionsHostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory) =>
            throw new NotImplementedException();
    }

    public partial interface IFunctionEndpoint
    {
        [ObsoleteEx(ReplacementTypeOrMember = "Process(Message, ExecutionContext, IMessageReceiver, ILogger)",
            TreatAsErrorFromVersion = "2",
            RemoveInVersion = "3")]
        Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null);
    }

    public partial class FunctionEndpoint
    {
        [ObsoleteEx(ReplacementTypeOrMember = "Process(Message, ExecutionContext, IMessageReceiver, ILogger)",
            TreatAsErrorFromVersion = "2",
            RemoveInVersion = "3")]
        public Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null) => throw new NotImplementedException();
    }

    [ObsoleteEx(ReplacementTypeOrMember = nameof(NServiceBusTriggerFunctionAttribute),
        TreatAsErrorFromVersion = "2",
        RemoveInVersion = "3")]
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class NServiceBusEndpointNameAttribute : Attribute
    {
        public string Name { get; }

        public string TriggerFunctionName { get; }

        public NServiceBusEndpointNameAttribute(string name)
        {
        }

        public NServiceBusEndpointNameAttribute(string name, string triggerFunctionName)
        {
        }
    }

    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        [ObsoleteEx(Message = "The static hosting model has been deprecated. Refer to the documentation for details on how to use class-instance approach instead.",
            RemoveInVersion = "3",
            TreatAsErrorFromVersion = "2")]
        public static ServiceBusTriggeredEndpointConfiguration FromAttributes() => throw new NotImplementedException();
    }
}