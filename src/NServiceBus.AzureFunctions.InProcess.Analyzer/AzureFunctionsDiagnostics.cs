namespace NServiceBus.AzureFunctions.InProcess.Analyzer
{
    using Microsoft.CodeAnalysis;

    public static class AzureFunctionsDiagnostics
    {
        public const string InvalidEndpointNameErrorId = "NSBFUNC001";
        public const string InvalidTriggerFunctionNameErrorId = "NSBFUNC002";
        public const string PurgeOnStartupNotAllowedId = "NSBFUNC003";
        public const string LimitMessageProcessingToNotAllowedId = "NSBFUNC004";
        public const string DefineCriticalErrorActionNotAllowedId = "NSBFUNC005";
        public const string SetDiagnosticsPathNotAllowedId = "NSBFUNC006";
        public const string MakeInstanceUniquelyAddressableNotAllowedId = "NSBFUNC007";
        public const string UseTransportNotAllowedId = "NSBFUNC008";
        public const string OverrideLocalAddressNotAllowedId = "NSBFUNC009";
        public const string RouteReplyToThisInstanceNotAllowedId = "NSBFUNC010";
        public const string RouteToThisInstanceNotAllowedId = "NSBFUNC011";
        public const string TransportTransactionModeNotAllowedId = "NSBFUNC012";
        public const string MaxAutoLockRenewalDurationNotAllowedId = "NSBFUNC013";
        public const string PrefetchCountNotAllowedId = "NSBFUNC014";
        public const string PrefetchMultiplierNotAllowedId = "NSBFUNC015";
        public const string TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId = "NSBFUNC016";
        public const string EntityMaximumSizeNotAllowedId = "NSBFUNC017";
        public const string EnablePartitioningNotAllowedId = "NSBFUNC018";

        const string DiagnosticCategory = "NServiceBus.AzureFunctions";

        internal static readonly DiagnosticDescriptor InvalidEndpointNameError = new DiagnosticDescriptor(
            id: InvalidEndpointNameErrorId,
            title: "Invalid Endpoint Name",
            messageFormat: "Endpoint name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidTriggerFunctionNameError = new DiagnosticDescriptor(
            id: InvalidTriggerFunctionNameErrorId,
            title: "Invalid Trigger Function Name",
            messageFormat: "Trigger function name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PurgeOnStartupNotAllowed = new DiagnosticDescriptor(
             id: PurgeOnStartupNotAllowedId,
             title: "PurgeOnStartup is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not support PurgeOnStartup.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor LimitMessageProcessingToNotAllowed = new DiagnosticDescriptor(
             id: LimitMessageProcessingToNotAllowedId,
             title: "LimitMessageProcessing is not supported in Azure Functions",
             messageFormat: "Concurrency-related settings are controlled via the Azure Function host.json configuration file.",
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
             messageFormat: "The NServiceBus endpoint address in Azure Functions is determined by the ServiceBusTrigger attribute.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor RouteReplyToThisInstanceNotAllowed = new DiagnosticDescriptor(
             id: RouteReplyToThisInstanceNotAllowedId,
             title: "RouteReplyToThisInstance is not supported in Azure Functions",
             messageFormat: "Azure Functions instances cannot be directly addressed as they have a highly volatile lifetime. Use 'RouteToThisEndpoint' instead.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor RouteToThisInstanceNotAllowed = new DiagnosticDescriptor(
             id: RouteToThisInstanceNotAllowedId,
             title: "RouteToThisInstance is not supported in Azure Functions",
             messageFormat: "Azure Functions instances cannot be directly addressed as they have a highly volatile lifetime. Use 'RouteToThisEndpoint' instead.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor MaxAutoLockRenewalDurationNotAllowed = new DiagnosticDescriptor(
             id: MaxAutoLockRenewalDurationNotAllowedId,
             title: "MaxAutoLockRenewalDuration is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot decide the lock renewal duration.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor PrefetchCountNotAllowed = new DiagnosticDescriptor(
             id: PrefetchCountNotAllowedId,
             title: "PrefetchCount is not supported in Azure Functions",
             messageFormat: "Message prefetching is controlled by the Azure Service Bus trigger and cannot be configured via the NServiceBus transport configuration API when using Azure Functions.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor PrefetchMultiplierNotAllowed = new DiagnosticDescriptor(
             id: PrefetchMultiplierNotAllowedId,
             title: "PrefetchMultiplier is not supported in Azure Functions",
             messageFormat: "Message prefetching is controlled by the Azure Service Bus trigger and cannot be configured via the NServiceBus transport configuration API when using Azure Functions",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor TimeToWaitBeforeTriggeringCircuitBreakerNotAllowed = new DiagnosticDescriptor(
             id: TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId,
             title: "TimeToWaitBeforeTriggeringCircuitBreaker is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not control the message receiver and cannot access circuit breaker settings.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor EntityMaximumSizeNotAllowed = new DiagnosticDescriptor(
             id: EntityMaximumSizeNotAllowedId,
             title: "EntityMaximumSize is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not support automatic queue creation.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor EnablePartitioningNotAllowed = new DiagnosticDescriptor(
             id: EnablePartitioningNotAllowedId,
             title: "EnablePartitioning is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints do not support automatic queue creation.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor TransportTransactionModeNotAllowed = new DiagnosticDescriptor(
             id: TransportTransactionModeNotAllowedId,
             title: "TransportTransactionMode is not supported in Azure Functions",
             messageFormat: "Transport TransactionMode is controlled by the Azure Service Bus trigger and cannot be configured via the NServiceBus transport configuration API when using Azure Functions.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );
    }
}