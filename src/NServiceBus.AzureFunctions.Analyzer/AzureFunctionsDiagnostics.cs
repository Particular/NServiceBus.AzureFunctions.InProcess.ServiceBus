namespace NServiceBus.AzureFunctions.Analyzer
{
    using Microsoft.CodeAnalysis;

    public static class AzureFunctionsDiagnostics
    {
        public const string PurgeOnStartupNotAllowedId = "NSBAF0001";
        public const string LimitMessageProcessingToNotAllowedId = "NSBAF0002";
        public const string DefineCriticalErrorActionNotAllowedId = "NSBAF0003";
        public const string SetDiagnosticsPathNotAllowedId = "NSBAF0004";
        public const string MakeInstanceUniquelyAddressableNotAllowedId = "NSBAF0005";
        public const string UseTransportNotAllowedId = "NSBAF0006";
        public const string OverrideLocalAddressNotAllowedId = "NSBAF0007";
        public const string RouteReplyToThisInstanceNotAllowedId = "NSBAF0008";
        public const string RouteToThisInstanceNotAllowedId = "NSBAF0009";

        const string DiagnosticCategory = "NServiceBus.AzureFunctions";

        internal static readonly DiagnosticDescriptor PurgeOnStartupNotAllowed = new DiagnosticDescriptor(
             id: PurgeOnStartupNotAllowedId,
             title: "PurgeOnStartup is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints are started when the first message arrives. PurgeOnStartup may purge whenever a new instance is started.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor LimitMessageProcessingToNotAllowed = new DiagnosticDescriptor(
             id: LimitMessageProcessingToNotAllowedId,
             title: "LimitMessageProcessing is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot limit message processing concurrency.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor DefineCriticalErrorActionNotAllowed = new DiagnosticDescriptor(
             id: DefineCriticalErrorActionNotAllowedId,
             title: "DefineCriticalErrorAction is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the application lifecycle and should not define behavior in the case of critical errors.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor SetDiagnosticsPathNotAllowed = new DiagnosticDescriptor(
             id: SetDiagnosticsPathNotAllowedId,
             title: "SetDiagnosticsPath is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints should not write diagnostics to the local file system. Use CustomDiagnosticsWriter to write diagnostics to another location.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor MakeInstanceUniquelyAddressableNotAllowed = new DiagnosticDescriptor(
             id: MakeInstanceUniquelyAddressableNotAllowedId,
             title: "MakeInstanceUniquelyAddressable is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints have unpredictable lifecycles and should not be uniquely addressable.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor UseTransportNotAllowed = new DiagnosticDescriptor(
             id: UseTransportNotAllowedId,
             title: "UseTransport is not supported in Azure Functions",
             messageFormat: "The package configures Azure Service Bus transport by default. Use ServiceBusTriggeredEndpointConfiguration.Transport to access the transport configuration.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Warning,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor OverrideLocalAddressNotAllowed = new DiagnosticDescriptor(
             id: OverrideLocalAddressNotAllowedId,
             title: "OverrideLocalAddress is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot decide the local address.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor RouteReplyToThisInstanceNotAllowed = new DiagnosticDescriptor(
             id: RouteReplyToThisInstanceNotAllowedId,
             title: "RouteReplyToThisInstance is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot configure receiver routing.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor RouteToThisInstanceNotAllowed = new DiagnosticDescriptor(
             id: RouteToThisInstanceNotAllowedId,
             title: "RouteToThisInstance is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot configure receiver routing.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );
    }
}