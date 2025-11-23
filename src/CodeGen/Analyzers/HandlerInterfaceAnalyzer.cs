using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Coordix.CodeGen.Analyzers;

/// <summary>
/// Analyzer that validates handler implementations
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerInterfaceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        CoordixAnalyzerDiagnostics.MissingHandleMethod,
        CoordixAnalyzerDiagnostics.NonPublicHandler
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        // Check if this type implements any Coordix handler interfaces
        System.Collections.Generic.List<INamedTypeSymbol> handlerInterfaces = namedTypeSymbol.Interfaces
            .Where(i => IsCoordixHandlerInterface(i))
            .ToList();

        if (!handlerInterfaces.Any())
        {
            return;
        }

        // Check if class is public
        if (namedTypeSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                CoordixAnalyzerDiagnostics.NonPublicHandler,
                namedTypeSymbol.Locations[0],
                namedTypeSymbol.Name
            );
            context.ReportDiagnostic(diagnostic);
        }

        // Check for Handle method
        foreach (INamedTypeSymbol? handlerInterface in handlerInterfaces)
        {
            if (!HasValidHandleMethod(namedTypeSymbol, handlerInterface))
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    CoordixAnalyzerDiagnostics.MissingHandleMethod,
                    namedTypeSymbol.Locations[0],
                    namedTypeSymbol.Name
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private bool IsCoordixHandlerInterface(INamedTypeSymbol interfaceSymbol)
    {
        string interfaceName = interfaceSymbol.Name;
        return interfaceName == "IRequestHandler" || interfaceName == "INotificationHandler";
    }

    private bool HasValidHandleMethod(INamedTypeSymbol typeSymbol, INamedTypeSymbol handlerInterface)
    {
        // Check if the type implements the Handle method from the interface
        IMethodSymbol handleMethod = typeSymbol.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.DeclaredAccessibility == Accessibility.Public);

        if (handleMethod == null)
        {
            return false;
        }

        // Verify it's actually implementing the interface method
        IMethodSymbol interfaceMethod = handlerInterface.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (interfaceMethod == null)
        {
            return false;
        }

        // Check if the implementation matches
        ISymbol? implementation = typeSymbol.FindImplementationForInterfaceMember(interfaceMethod);
        return implementation != null && implementation.Equals(handleMethod, SymbolEqualityComparer.Default);
    }
}

