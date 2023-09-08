namespace NServiceBus.AzureFunctions.Analyzer
{
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

            if (context.Node is InvocationExpressionSyntax invocationExpression)
            {
                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression)
                {
                    if (memberAccessExpression.Name.Identifier.Text == "PurgeOnStartup")
                    {
                        if (memberAccessExpression.Expression is MemberAccessExpressionSyntax parentMemberAccessExpression)
                        {
                            if (parentMemberAccessExpression.Name.Identifier.Text == "AdvancedConfiguration")
                            {
                                context.ReportDiagnostic(AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed, invocationExpression);
                            }
                        }
                    }
                }
            }
        }
    }
}