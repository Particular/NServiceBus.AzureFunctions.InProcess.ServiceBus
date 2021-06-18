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
    public class SourceGeneratorApprovals
    {
        [Test]
        public void UsingNamespace()
        {
            var source =
@"using NServiceBus;

[assembly: NServiceBusEndpointName(Foo.Startup.EndpointName)]

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
@"[assembly: NServiceBus.NServiceBusEndpointName(Foo.Startup.EndpointName)]

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
            var source = @"[assembly: NServiceBus.NServiceBusEndpointName(""endpoint"")]";
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

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Invalid_name_should_cause_an_error(string endpointName)
        {
            var source = @"
using NServiceBus;

[assembly: NServiceBusEndpointName(""" + endpointName + @""")]
";
            var (output, diagnostics) = GetGeneratedOutput(source, suppressGeneratedDiagnosticsErrors: true);

            Assert.True(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
        }


        [OneTimeSetUp]
        public void Init()
        {
            // For the unit tests to work, the compilation used by the source generator needs to know that NServiceBusEndpointName
            // is an attribute from NServiceBus namespace and its full name is NServiceBus.NServiceBusEndpointNameAttribute.
            // By referencing NServiceBusEndpointNameAttribute here, NServiceBus.AzureFunctions.InProcess.ServiceBus is forced to load and participate in the compilation.
            _ = new NServiceBusEndpointNameAttribute("test");
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

            var generator = new TriggerFunctionGenerator();

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