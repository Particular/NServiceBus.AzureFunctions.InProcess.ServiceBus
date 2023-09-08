namespace NServiceBus.AzureFunctions.Analyzer
{
    using System;
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
            AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed
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

            if (memberAccessExpression.Name.Identifier.Text != "PurgeOnStartup")
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
                context.ReportDiagnostic(AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed, invocationExpression);
            }
        }
    }
}