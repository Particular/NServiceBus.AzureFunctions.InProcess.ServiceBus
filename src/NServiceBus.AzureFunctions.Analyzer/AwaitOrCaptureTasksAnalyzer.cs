namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitOrCaptureTasksAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NSB0001";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

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
                        if (memberAccessExpression.Expression is MemberAccessExpressionSyntax
                            parentMemberAccessExpression)
                        {
                            if (parentMemberAccessExpression.Name.Identifier.Text == "AdvancedConfiguration")
                            {
                                context.ReportDiagnostic(Diagnostic.Create(diagnostic, invocationExpression.GetLocation(), invocationExpression.ToString()));
                                //        return;
                            }
                        }
                    }
                }
            }
            //obj.AdvancedConfiguration.PurgeOnStartup
            //exp[obj.AdvancedConfiguration]+DotToken[.]+Identifier[PurgeOnStartup]
            //

            //if (context.Node is InvocationExpressionSyntax call)
            //{
            //    if (call.Expression is MemberAccessExpressionSyntax expression)
            //    {
            //        expression.OperatorToken == SyntaxToken
            //    }
            //    call.Expression
            //}
            //if (!(context.Node is InvocationExpressionSyntax call))
            //{
            //    return;
            //}

            //if (!(call.Parent is ExpressionStatementSyntax))
            //{
            //    return;
            //}

            //foreach (var token in call.Expression?.DescendantTokens() ?? Enumerable.Empty<SyntaxToken>())
            //{
            //    if (context.CancellationToken.IsCancellationRequested)
            //    {
            //        return;
            //    }

            //    // check syntax tree (cheap) first for possible call requiring await and then check semantic model (expensive) to confirm
            //    if (CouldBeMethodRequiringAwait(token) && IsMethodRequiringAwait(call, context))
            //    {
            //        context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
            //        return;
            //    }
            //}
        }

        static readonly DiagnosticDescriptor diagnostic = new DiagnosticDescriptor(
            DiagnosticId,
            "NServiceBus Azure Functions",
            "Usage of not allowed configuration options",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "This API is not supported in Azure Functions.");
    }
}