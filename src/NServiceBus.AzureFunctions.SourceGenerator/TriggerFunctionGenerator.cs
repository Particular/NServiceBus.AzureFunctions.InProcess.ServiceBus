﻿namespace NServiceBus.AzureFunctions.SourceGenerator
{
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class TriggerFunctionGenerator : ISourceGenerator
    {
        internal static readonly DiagnosticDescriptor InvalidEndpointNameError = new DiagnosticDescriptor(id: "NSBFUNC001",
            title: "Invalid Endpoint Name",
            messageFormat: "Endpoint name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidTriggerFunctionNameError = new DiagnosticDescriptor(id: "NSBFUNC002",
            title: "Invalid Trigger Function Name",
            messageFormat: "Trigger function name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            internal string endpointName;
            internal string triggerFunctionName;
            internal bool attributeFound;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is AttributeSyntax attributeSyntax
                    && IsNServiceBusEndpointNameAttribute(context.SemanticModel.GetTypeInfo(attributeSyntax).Type?.ToDisplayString()))
                {
                    attributeFound = true;
                    endpointName = AttributeParameterAtPosition(0);
                    triggerFunctionName = AttributeParametersCount() == 2
                        ? AttributeParameterAtPosition(1)
                        : $"NServiceBusFunctionEndpointTrigger-{endpointName}";
                }

                bool IsNServiceBusEndpointNameAttribute(string value) => value.Equals("NServiceBus.NServiceBusEndpointNameAttribute");
                string AttributeParameterAtPosition(int position) => context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[position].Expression).ToString();
                int AttributeParametersCount() => attributeSyntax.ArgumentList.Arguments.Count;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Short circuit if this is a different syntax receiver
            if (!(context.SyntaxContextReceiver is SyntaxReceiver syntaxReceiver))
            {
                return;
            }

            // Skip processing if no attribute was found
            if (!syntaxReceiver.attributeFound)
            {
                return;
            }

            // Generate an error if empty/null/space is used as endpoint name
            if (string.IsNullOrWhiteSpace(syntaxReceiver.endpointName))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointNameError, Location.None, syntaxReceiver.endpointName));
                return;
            }

            // Generate an error if empty/null/space is used as trigger function name
            if (string.IsNullOrWhiteSpace(syntaxReceiver.triggerFunctionName))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidTriggerFunctionNameError, Location.None, syntaxReceiver.triggerFunctionName));
                return;
            }

            var source =
$@"// <autogenerated/>
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

class FunctionEndpointTrigger
{{
    readonly IFunctionEndpoint endpoint;

    public FunctionEndpointTrigger(IFunctionEndpoint endpoint)
    {{
        this.endpoint = endpoint;
    }}

    [FunctionName(""{syntaxReceiver.triggerFunctionName}"")]
    public async Task Run(
        [ServiceBusTrigger(queueName: ""{syntaxReceiver.endpointName}"")]
        Message message,
        ILogger logger
        CancellationToken cancellationToken)
    {{
        await endpoint.Process(message, logger, cancellationToken);
    }}
}}";
            context.AddSource("NServiceBus__FunctionEndpointTrigger", SourceText.From(source, Encoding.UTF8));
        }
    }
}