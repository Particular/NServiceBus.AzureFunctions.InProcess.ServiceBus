namespace NServiceBus.AzureFunctions.SourceGenerator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class SourceGeneratorApprovals2
    {
        [Test]
        public void UsingNamespace()
        {
            var source =
@"using NServiceBus;

[assembly: NServiceBusTriggerFunction(Foo.Startup.EndpointName)]

namespace Foo
{
    public class Startup
    {
        public const string EndpointName = ""endpoint"";
    }
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void UsingFullyQualifiedAttributeName()
        {
            var source =
@"[assembly: NServiceBus.NServiceBusTriggerFunction(Foo.Startup.EndpointName)]

namespace Foo
{
    public class Startup
    {
        public const string EndpointName = ""endpoint"";
    }
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void NameIsStringValue()
        {
            var source = @"[assembly: NServiceBus.NServiceBusTriggerFunction(""endpoint"")]";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void No_attribute_should_not_generate_trigger_function()
        {
            var source = @"";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void No_attribute_should_not_generate_compilation_error()
        {
            var source = @"using NServiceBus;";
            var (output, diagnostics) = GetGeneratedOutput(source);

            Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Test]
        public void Can_override_trigger_function_name()
        {
            var source =
                @"using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", ""trigger"")]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Invalid_name_should_cause_an_error(string endpointName)
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""" + endpointName + @""")]
";
            var (output, diagnostics) = GetGeneratedOutput(source, suppressGeneratedDiagnosticsErrors: true);

            Assert.True(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.Id == TriggerFunctionGenerator2.InvalidEndpointNameError.Id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Invalid_trigger_function_name_should_cause_an_error(string triggerFunctionName)
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", """ + triggerFunctionName + @""")]
";
            var (output, diagnostics) = GetGeneratedOutput(source, suppressGeneratedDiagnosticsErrors: true);

            Assert.True(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.Id == TriggerFunctionGenerator2.InvalidTriggerFunctionNameError.Id));
        }

        [Test]
        public void Use_one_optional()
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", ""trigger"")]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void Use_two_optionals()
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", ""trigger"", true)]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void One_optional_with_parameter_name()
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", triggerFunctionName: ""trigger"")]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void One_optional_out_of_order()
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", EnableCrossEntityTransactions = true)]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [Test]
        public void Two_optionals_out_of_order()
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", EnableCrossEntityTransactions = true, TriggerFunctionName = ""trigger"")]

public class Startup
{
}";
            var (output, _) = GetGeneratedOutput(source);

            Approver.Verify(output);
        }

        [OneTimeSetUp]
        public void Init()
        {
            // For the unit tests to work, the compilation used by the source generator needs to know that NServiceBusTriggerFunction
            // is an attribute from NServiceBus namespace and its full name is NServiceBus.NServiceBusTriggerFunctionAttribute.
            // By referencing NServiceBusTriggerFunctionAttribute here, NServiceBus.AzureFunctions.InProcess.ServiceBus is forced to load and participate in the compilation.
            _ = new NServiceBusTriggerFunctionAttribute(name: "test");
            _ = new NServiceBusTriggerFunctionAttribute(name: "test", triggerFunctionName: "trigger");
            _ = new NServiceBusTriggerFunctionAttribute(name: "test", enableCrossEntityTransactions: true);
            _ = new NServiceBusTriggerFunctionAttribute(name: "test", triggerFunctionName: "trigger", enableCrossEntityTransactions: true);
            _ = new NServiceBusTriggerFunctionAttribute(name: "test", enableCrossEntityTransactions: true, triggerFunctionName: "trigger");
        }

        static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source, bool suppressGeneratedDiagnosticsErrors = false)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new List<MetadataReference>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            var compilation = CSharpCompilation.Create("foo", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Fail tests when the injected program isn't valid _before_ running generators
            var compileDiagnostics = compilation.GetDiagnostics();
            Assert.False(compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + compileDiagnostics.FirstOrDefault()?.GetMessage());

            var generator = new TriggerFunctionGenerator2();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            if (!suppressGeneratedDiagnosticsErrors)
            {
                Assert.False(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
            }

            return (outputCompilation.SyntaxTrees.Last().ToString(), generateDiagnostics);
        }
    }
}