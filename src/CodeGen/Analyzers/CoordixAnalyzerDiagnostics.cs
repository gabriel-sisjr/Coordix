using Microsoft.CodeAnalysis;

namespace Coordix.CodeGen.Analyzers;

/// <summary>
/// Diagnostic descriptors for Coordix analyzers
/// </summary>
internal static class CoordixAnalyzerDiagnostics
{
    private const string Category = "Coordix";

    public static readonly DiagnosticDescriptor CodeGenModeWithoutPackage = new DiagnosticDescriptor(
        id: "COORDIX001",
        title: "CodeGenPreferred mode requires Coordix.CodeGen package",
        messageFormat: "HandlerResolutionMode.CodeGenPreferred is configured but Coordix.CodeGen package is not referenced. Either install Coordix.CodeGen or use HandlerResolutionMode.Reflection.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When using HandlerResolutionMode.CodeGenPreferred, the Coordix.CodeGen package must be installed to generate optimized handler code."
    );

    public static readonly DiagnosticDescriptor MissingHandleMethod = new DiagnosticDescriptor(
        id: "COORDIX002",
        title: "Handler missing public Handle method",
        messageFormat: "Handler '{0}' must implement a public Handle method matching its interface contract",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All handlers must implement a public Handle method with the correct signature."
    );

    public static readonly DiagnosticDescriptor DuplicateHandler = new DiagnosticDescriptor(
        id: "COORDIX003",
        title: "Duplicate handler registration detected",
        messageFormat: "Multiple handlers found for request type '{0}'. Only one handler is allowed for requests with responses.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple handlers registered for the same request type may cause ambiguous behavior.",
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd }
    );

    public static readonly DiagnosticDescriptor HandlerNotRegistered = new DiagnosticDescriptor(
        id: "COORDIX004",
        title: "Handler not registered in DI container",
        messageFormat: "Handler '{0}' implements a handler interface but is not registered in the DI container",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Handlers must be registered in the dependency injection container to be invoked by the mediator."
    );

    public static readonly DiagnosticDescriptor NonPublicHandler = new DiagnosticDescriptor(
        id: "COORDIX005",
        title: "Handler class should be public",
        messageFormat: "Handler '{0}' should be public to ensure proper code generation and DI registration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Handler classes should typically be public for proper dependency injection and code generation."
    );
}

