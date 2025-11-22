using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Coordix.CodeGen.Analyzers;

/// <summary>
/// Analyzer that detects duplicate handler registrations
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DuplicateHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        CoordixAnalyzerDiagnostics.DuplicateHandler
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // Find all handler types in the compilation
        Dictionary<string, List<(INamedTypeSymbol Handler, Location Location)>> handlerRegistrations = new Dictionary<string, List<(INamedTypeSymbol Handler, Location Location)>>();

        foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            SyntaxNode root = syntaxTree.GetRoot(context.CancellationToken);

            // Find all class declarations
            IEnumerable<ClassDeclarationSyntax> classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (ClassDeclarationSyntax classDecl in classDeclarations)
            {
                INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null)
                {
                    continue;
                }

                // Check if this class implements IRequestHandler with response
                List<INamedTypeSymbol> requestHandlerInterfaces = classSymbol.Interfaces
                    .Where(i => i.Name == "IRequestHandler" && i.TypeArguments.Length == 2)
                    .ToList();

                foreach (INamedTypeSymbol? handlerInterface in requestHandlerInterfaces)
                {
                    // Get the request type (first type argument)
                    ITypeSymbol requestType = handlerInterface.TypeArguments[0];
                    string requestTypeKey = requestType.ToDisplayString();

                    if (!handlerRegistrations.ContainsKey(requestTypeKey))
                    {
                        handlerRegistrations[requestTypeKey] = new List<(INamedTypeSymbol, Location)>();
                    }

                    handlerRegistrations[requestTypeKey].Add((classSymbol, classDecl.Identifier.GetLocation()));
                }
            }
        }

        // Report duplicates
        foreach (KeyValuePair<string, List<(INamedTypeSymbol Handler, Location Location)>> kvp in handlerRegistrations)
        {
            if (kvp.Value.Count > 1)
            {
                foreach ((INamedTypeSymbol handler, Location location) in kvp.Value)
                {
                    Diagnostic diagnostic = Diagnostic.Create(
                        CoordixAnalyzerDiagnostics.DuplicateHandler,
                        location,
                        kvp.Key
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}

