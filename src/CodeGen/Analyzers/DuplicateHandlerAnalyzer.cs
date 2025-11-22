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
        // Find all handler types in the compilation using symbol-based approach
        // This avoids RS1030 warning by not using GetSemanticModel()
        Dictionary<string, List<(INamedTypeSymbol Handler, Location Location)>> handlerRegistrations = new Dictionary<string, List<(INamedTypeSymbol Handler, Location Location)>>();

        // Get all types in the compilation
        INamedTypeSymbol? requestHandlerInterface = context.Compilation.GetTypeByMetadataName("Coordix.Interfaces.IRequestHandler`2");
        if (requestHandlerInterface == null)
        {
            return;
        }

        // Visit all types in the compilation
        foreach (INamedTypeSymbol type in GetAllTypes(context.Compilation.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class || type.IsAbstract)
            {
                continue;
            }

            // Check if this class implements IRequestHandler with response
            List<INamedTypeSymbol> requestHandlerInterfaces = type.Interfaces
                .Where(i => i.OriginalDefinition.Equals(requestHandlerInterface, SymbolEqualityComparer.Default))
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

                // Get location from the type's syntax reference
                Location? location = type.Locations.FirstOrDefault();
                if (location != null)
                {
                    handlerRegistrations[requestTypeKey].Add((type, location));
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

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }

        foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (INamedTypeSymbol type in GetAllTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }
}

