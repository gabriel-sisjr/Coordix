using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Coordix.CodeGen.Analyzers;

/// <summary>
/// Analyzer that validates Coordix configuration
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CodeGenConfigurationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        CoordixAnalyzerDiagnostics.CodeGenModeWithoutPackage
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a call to AddCoordix
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol == null || methodSymbol.Name != "AddCoordix")

        /* Unmerged change from project 'CodeGen(netstandard2.1)'
        Before:
                    return;
        After:
                {
                    return;
                }
        */

        /* Unmerged change from project 'CodeGen(netstandard2.1)'
        Before:
                    return;

                // Look for lambda or options configuration
                var arguments = invocation.ArgumentList?.Arguments;
        After:
                {
                    return;
                }

                // Look for lambda or options configuration
                SeparatedSyntaxList<ArgumentSyntax>? arguments = invocation.ArgumentList?.Arguments;
        */
        {
            return;
        }

        // Check if the containing namespace is Coordix
        if (!IsCoordixExtensionMethod(methodSymbol))
        {
            return;
        }

        // Look for lambda or options configuration
        SeparatedSyntaxList<ArgumentSyntax>? arguments = invocation.ArgumentList?.Arguments;
        if (arguments == null || arguments.Value.Count == 0)
        {
            return;
        }

        // Analyze the configuration lambda/options
        foreach (ArgumentSyntax argument in arguments.Value)
        {
            if (argument.Expression is SimpleLambdaExpressionSyntax lambda)
            {
                AnalyzeLambdaForCodeGenMode(context, lambda);
            }
        }
    }

    private void AnalyzeLambdaForCodeGenMode(SyntaxNodeAnalysisContext context, SimpleLambdaExpressionSyntax lambda)
    {
        System.Collections.Generic.IEnumerable<SyntaxNode> descendantNodes = lambda.DescendantNodes();

        foreach (SyntaxNode node in descendantNodes)
        {
            // Look for: options.ResolutionMode = HandlerResolutionMode.CodeGenPreferred
            if (node is AssignmentExpressionSyntax assignment)
            {
                ExpressionSyntax rightSide = assignment.Right;

                // Check if right side contains CodeGenPreferred
                if (rightSide.ToString().Contains("CodeGenPreferred"))
                {
                    // Check if CodeGen package is referenced
                    if (!IsCodeGenPackageReferenced(context.Compilation))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(
                            CoordixAnalyzerDiagnostics.CodeGenModeWithoutPackage,
                            assignment.GetLocation()
                        );
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private bool IsCoordixExtensionMethod(IMethodSymbol methodSymbol)
    {
        string? containingNamespace = methodSymbol.ContainingNamespace?.ToDisplayString();
        return containingNamespace != null && containingNamespace.StartsWith("Coordix");
    }

    private bool IsCodeGenPackageReferenced(Compilation compilation)
    {
        // Check if any type from Coordix.CodeGen namespace exists
        ImmutableArray<INamedTypeSymbol> codeGenTypes = compilation.GetTypesByMetadataName("Coordix.CodeGen.CoordixSourceGenerator");

        if (codeGenTypes != null)
        {
            return true;
        }

        // Alternative: check for any type in Coordix.CodeGen namespace
        ImmutableArray<INamedTypeSymbol> coordixCodeGenNamespace = compilation.GetTypesByMetadataName("Coordix.CodeGen.Extensions.ServiceCollectionExtensions");
        return coordixCodeGenNamespace != null;
    }
}

