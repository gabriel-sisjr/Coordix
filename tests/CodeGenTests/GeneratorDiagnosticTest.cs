using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Coordix.CodeGen.Tests;

public class GeneratorDiagnosticTest
{
    private readonly ITestOutputHelper _output;

    public GeneratorDiagnosticTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Generator_Debug_ShouldShowWhatHappens()
    {
        // Arrange
        string source = @"
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace TestApp
{
	public class TestRequest : IRequest<string> { }

	public class TestRequestHandler : IRequestHandler<TestRequest, string>
	{
		public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
		{
			return Task.FromResult(""Hello"");
		}
	}
}";

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IServiceProvider).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Coordix.Interfaces.IRequest).Assembly.Location),
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CoordixSourceGenerator generator = new CoordixSourceGenerator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        _output.WriteLine("Running generator...");

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation? outputCompilation,
            out System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics);

        _output.WriteLine($"Generator diagnostics: {diagnostics.Length}");
        foreach (Diagnostic diag in diagnostics)
        {
            _output.WriteLine($"  - {diag.Severity}: {diag.GetMessage()}");
        }

        _output.WriteLine($"Generated files: {outputCompilation.SyntaxTrees.Count() - compilation.SyntaxTrees.Count()}");

        foreach (SyntaxTree tree in outputCompilation.SyntaxTrees)
        {
            if (!compilation.SyntaxTrees.Contains(tree))
            {
                _output.WriteLine($"Generated file: {tree.FilePath}");
                _output.WriteLine(tree.ToString());
            }
        }

        GeneratorDriverRunResult result = driver.GetRunResult();
        _output.WriteLine($"Generator ran: {result.GeneratedTrees.Length} trees generated");
        foreach (SyntaxTree generatedTree in result.GeneratedTrees)
        {
            _output.WriteLine($"  - {generatedTree.FilePath}");
            _output.WriteLine("");
            _output.WriteLine("=== Generated Code ===");
            _output.WriteLine(generatedTree.ToString());
            _output.WriteLine("=== End Generated Code ===");
        }

        // Debug: check what symbols are in the compilation
        _output.WriteLine("\nSymbols in compilation:");
        SemanticModel semanticModel = outputCompilation.GetSemanticModel(syntaxTree);
        SyntaxNode root = syntaxTree.GetRoot();
        Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax? classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().FirstOrDefault();

        if (classDecl != null)
        {
            INamedTypeSymbol? symbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (symbol is Microsoft.CodeAnalysis.INamedTypeSymbol namedType)
            {
                _output.WriteLine($"Class: {namedType.Name}");
                _output.WriteLine($"Is Abstract: {namedType.IsAbstract}");
                _output.WriteLine($"Is Generic: {namedType.IsGenericType}");
                _output.WriteLine($"Interfaces implemented: {namedType.AllInterfaces.Length}");

                foreach (INamedTypeSymbol iface in namedType.AllInterfaces)
                {
                    _output.WriteLine($"  - {iface.ToDisplayString()}");
                    _output.WriteLine($"    Original: {iface.OriginalDefinition.ToDisplayString()}");
                }
            }
        }
    }
}

