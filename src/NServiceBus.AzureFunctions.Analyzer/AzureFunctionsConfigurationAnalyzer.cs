namespace NServiceBus.AzureFunctions.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NServiceBus.AzureFunctions.Analyzer.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AzureFunctionsConfigurationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed,
            AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowed
        );

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocationExpression))
            {
                return;
            }

            if (!(invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression))
            {
                return;
            }

            if (!NotAllowedEndpointConfigurationMethods.TryGetValue(memberAccessExpression.Name.Identifier.Text, out var diagnosticDescriptor))
            {
                return;
            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);

            if (!(memberAccessSymbol.Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            if (methodSymbol.ReceiverType.ToString() == "NServiceBus.EndpointConfiguration")
            {
                context.ReportDiagnostic(diagnosticDescriptor, invocationExpression);
            }
        }

        static readonly Dictionary<string, DiagnosticDescriptor> NotAllowedEndpointConfigurationMethods
            = new Dictionary<string, DiagnosticDescriptor>
            {
                ["PurgeOnStartup"] = AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed,
                ["LimitMessageProcessingConcurrencyTo"] = AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowed
            };
    }
}