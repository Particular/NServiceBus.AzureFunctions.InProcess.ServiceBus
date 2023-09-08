namespace NServiceBus.AzureFunctions.Analyzer
{
    using Microsoft.CodeAnalysis;

    public static class AzureFunctionsDiagnostics
    {
        public const string PurgeOnStartupNotAllowedId = "NSBAF0001";

        const string DiagnosticCategory = "NServiceBus.AzureFunctions";

        internal static readonly DiagnosticDescriptor PurgeOnStartupNotAllowed = new DiagnosticDescriptor(
             id: PurgeOnStartupNotAllowedId,
             title: "PurgeOnStartup is not supported in Azure Functions",
             messageFormat: "Azure Functions endpoints are started when the first message arrives. PurgeOnStartup may purge whenever a new instance is started.",
             category: DiagnosticCategory,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true
            );
    }
}