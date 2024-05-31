namespace NServiceBus.AzureFunctions.SourceGenerator
{
    using System.Linq;
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
            internal string connection;
            internal bool enableCrossEntityTransactions;
            internal bool attributeFound;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is AttributeSyntax attributeSyntax
                    && IsNServiceBusEndpointNameAttribute(context.SemanticModel.GetTypeInfo(attributeSyntax).Type?.ToDisplayString()))
                {
                    attributeFound = true;

                    // Assign guaranteed endpoint/queue name and handle the defaults
                    endpointName = AttributeParameterAtPosition(0);
                    triggerFunctionName = $"NServiceBusFunctionEndpointTrigger-{endpointName}";
                    enableCrossEntityTransactions = false;

                    var attributeParametersCount = AttributeParametersCount();

                    if (attributeParametersCount == 1)
                    {
                        return;
                    }

                    var triggerFunctionNameAttribute = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.GetFirstToken().ValueText == "TriggerFunctionName");
                    if (triggerFunctionNameAttribute != null)
                    {
                        triggerFunctionName = context.SemanticModel.GetConstantValue(triggerFunctionNameAttribute.Expression).Value?.ToString();
                    }

                    var connectionAttribute = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.GetFirstToken().ValueText == "Connection");
                    if (connectionAttribute != null)
                    {
                        connection = context.SemanticModel.GetConstantValue(connectionAttribute.Expression).Value?.ToString();
                    }

                    var enableCrossEntityTransactionsAttribute = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.GetFirstToken().ValueText == "SendsAtomicWithReceive");
                    if (enableCrossEntityTransactionsAttribute != null)
                    {
                        enableCrossEntityTransactions = bool.Parse(context.SemanticModel.GetConstantValue(enableCrossEntityTransactionsAttribute.Expression).Value?.ToString());
                    }
                }

                bool IsNServiceBusEndpointNameAttribute(string value) => value?.Equals("NServiceBus.NServiceBusTriggerFunctionAttribute") ?? false;
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

            var source = syntaxReceiver.enableCrossEntityTransactions ?
                   AtomicSource(syntaxReceiver.triggerFunctionName, syntaxReceiver.endpointName, syntaxReceiver.connection) :
                   NonAtomicSource(syntaxReceiver.triggerFunctionName, syntaxReceiver.endpointName, syntaxReceiver.connection);

            context.AddSource("NServiceBus__FunctionEndpointTrigger", SourceText.From(source, Encoding.UTF8));
        }


        string AtomicSource(string triggerFunctionName, string endpointName, string connection)
        {
            var connectionParam = string.IsNullOrWhiteSpace(connection)
                ? ""
                : $", Connection=\"{connection}\"";
            return
    $@"// <autogenerated/>
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using NServiceBus;

class FunctionEndpointTrigger
{{
    readonly IFunctionEndpoint endpoint;

    public FunctionEndpointTrigger(IFunctionEndpoint endpoint)
    {{
        this.endpoint = endpoint;
    }}

    [FunctionName(""{triggerFunctionName}"")]
    public Task Run(
        [ServiceBusTrigger(queueName: ""{endpointName}"", AutoCompleteMessages = false{connectionParam})]
        ServiceBusReceivedMessage message,
        ServiceBusClient client,
        ServiceBusMessageActions messageActions,
        ILogger logger,
        ExecutionContext executionContext,
        CancellationToken cancellationToken)
    {{
        return endpoint.ProcessAtomic(message, executionContext, client, messageActions, logger, cancellationToken);
    }}
}}";
        }

        string NonAtomicSource(string triggerFunctionName, string endpointName, string connection)
        {
            var connectionParam = string.IsNullOrWhiteSpace(connection)
                ? ""
                : $", Connection=\"{connection}\"";
            return
    $@"// <autogenerated/>
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using NServiceBus;

class FunctionEndpointTrigger
{{
    readonly IFunctionEndpoint endpoint;

    public FunctionEndpointTrigger(IFunctionEndpoint endpoint)
    {{
        this.endpoint = endpoint;
    }}

    [FunctionName(""{triggerFunctionName}"")]
    public Task Run(
        [ServiceBusTrigger(queueName: ""{endpointName}"", AutoCompleteMessages = true{connectionParam})]
        ServiceBusReceivedMessage message,
        ILogger logger,
        ExecutionContext executionContext,
        CancellationToken cancellationToken)
    {{
        return endpoint.ProcessNonAtomic(message, executionContext, logger, cancellationToken);
    }}
}}";
        }
    }
}