using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Coordix.CodeGen;

/// <summary>
/// Incremental source generator that discovers handlers implementing Coordix interfaces
/// and generates an optimized HandlerExecutor without reflection.
/// </summary>
[Generator]
public class CoordixSourceGenerator : IIncrementalGenerator
{
    private const string IRequestHandlerWithResponseFullName = "Coordix.Interfaces.IRequestHandler<TRequest, TResponse>";
    private const string IRequestHandlerWithoutResponseFullName = "Coordix.Interfaces.IRequestHandler<TRequest>";
    private const string INotificationHandlerFullName = "Coordix.Interfaces.INotificationHandler<TNotification>";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter candidate class declarations that might be handlers
        IncrementalValuesProvider<INamedTypeSymbol?> candidateClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsCandidateClass(node),
                static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        // Combine with compilation to access symbol information
        IncrementalValueProvider<(Compilation Left, ImmutableArray<INamedTypeSymbol?> Right)> compilationAndClasses =
            context.CompilationProvider.Combine(candidateClasses.Collect());

        // Register source output
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        // Quick syntactic filter: class declaration with base list
        return node is ClassDeclarationSyntax { BaseList: not null } classDecl
            && !classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword);
    }

    private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        ClassDeclarationSyntax classDecl = (ClassDeclarationSyntax)context.Node;
        INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (symbol is not INamedTypeSymbol namedType)
        {
            return null;
        }

        // Filter: must be concrete class
        if (namedType.IsAbstract || namedType.TypeKind != TypeKind.Class)
        {
            return null;
        }

        // Filter: must not be generic (open generic types can't be instantiated)
        if (namedType.IsGenericType && !namedType.IsUnboundGenericType &&
            namedType.TypeArguments.Any(t => t.Kind == SymbolKind.TypeParameter))
        {
            return null;
        }

        // Check if implements any of our target interfaces
        if (!ImplementsCoordixHandlerInterface(namedType))
        {
            return null;
        }

        return namedType;
    }

    private static bool ImplementsCoordixHandlerInterface(INamedTypeSymbol type)
    {
        foreach (INamedTypeSymbol? iface in type.AllInterfaces)
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            string originalDef = iface.OriginalDefinition.ToDisplayString();

            if (originalDef == IRequestHandlerWithResponseFullName ||
                originalDef == IRequestHandlerWithoutResponseFullName ||
                originalDef == INotificationHandlerFullName)
            {
                return true;
            }
        }

        return false;
    }

    private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> classes,
        SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        List<HandlerInfo> handlers = new List<HandlerInfo>();

        foreach (INamedTypeSymbol? handlerSymbol in classes)
        {
            if (handlerSymbol is null)
            {
                continue;
            }

            foreach (INamedTypeSymbol? iface in handlerSymbol.AllInterfaces)
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }

                string originalDef = iface.OriginalDefinition.ToDisplayString();

                if (originalDef == IRequestHandlerWithResponseFullName)
                {
                    // IRequestHandler<TRequest, TResponse>
                    string requestType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    string responseType = iface.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    handlers.Add(new HandlerInfo(
                        handlerSymbol.Name,
                        handlerSymbol.ContainingNamespace.ToDisplayString(),
                        HandlerKind.RequestWithResponse,
                        requestType,
                        responseType));
                }
                else if (originalDef == IRequestHandlerWithoutResponseFullName)
                {
                    // IRequestHandler<TRequest>
                    string requestType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    handlers.Add(new HandlerInfo(
                        handlerSymbol.Name,
                        handlerSymbol.ContainingNamespace.ToDisplayString(),
                        HandlerKind.RequestWithoutResponse,
                        requestType));
                }
                else if (originalDef == INotificationHandlerFullName)
                {
                    // INotificationHandler<TNotification>
                    string notificationType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    handlers.Add(new HandlerInfo(
                        handlerSymbol.Name,
                        handlerSymbol.ContainingNamespace.ToDisplayString(),
                        HandlerKind.Notification,
                        notificationType));
                }
            }
        }

        if (handlers.Count == 0)
        {
            return;
        }

        // Generate the HandlerExecutor code
        string sourceText = GenerateHandlerExecutor(handlers);
        context.AddSource("GeneratedHandlerExecutor.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    private static string GenerateHandlerExecutor(List<HandlerInfo> handlers)

    /* Unmerged change from project 'CodeGen(netstandard2.1)'
    Before:
            var sb = new StringBuilder();
    After:
            StringBuilder sb = new StringBuilder();
    */
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Coordix.Interfaces;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();

        // Collect unique namespaces from handlers
        IOrderedEnumerable<string> namespaces = handlers
            .Select(h => h.HandlerNamespace)
            .Distinct()
            .OrderBy(ns => ns);

        foreach (string? ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine("namespace Coordix.Implementation");
        sb.AppendLine("{");
        sb.AppendLine("\t/// <summary>");
        sb.AppendLine("\t/// Generated handler executor that uses compile-time code generation instead of reflection.");
        sb.AppendLine("\t/// This class is automatically generated by Coordix.CodeGen source generator.");
        sb.AppendLine("\t/// </summary>");
        sb.AppendLine("\tpublic sealed class GeneratedHandlerExecutor : IHandlerExecutor");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tprivate readonly IServiceProvider _provider;");
        sb.AppendLine();
        sb.AppendLine("\t\tpublic GeneratedHandlerExecutor(IServiceProvider provider)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\t_provider = provider ?? throw new ArgumentNullException(nameof(provider));");
        sb.AppendLine("\t\t}");
        sb.AppendLine();

        // Generate ExecuteRequestHandler<TResponse>
        GenerateExecuteRequestHandlerWithResponse(sb, handlers);
        sb.AppendLine();

        // Generate ExecuteRequestHandler (no response)
        GenerateExecuteRequestHandlerWithoutResponse(sb, handlers);
        sb.AppendLine();

        // Generate ExecuteNotificationHandler<TNotification>
        GenerateExecuteNotificationHandler(sb, handlers);
        sb.AppendLine();

        // Generate dynamic execution methods for background worker support
        GenerateDynamicExecutionMethods(sb);

        sb.AppendLine("\t}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateExecuteRequestHandlerWithResponse(StringBuilder sb, List<HandlerInfo> handlers)

    /* Unmerged change from project 'CodeGen(netstandard2.1)'
    Before:
            var requestsWithResponse = handlers
    After:
            List<HandlerInfo> requestsWithResponse = handlers
    */
    {
        List<HandlerInfo> requestsWithResponse = handlers
            .Where(h => h.Kind == HandlerKind.RequestWithResponse)
            .ToList();

        sb.AppendLine(
            "\t\tpublic async Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tvar requestType = request.GetType();");
        sb.AppendLine();

        if (requestsWithResponse.Count > 0)
        {
            for (int i = 0; i < requestsWithResponse.Count; i++)
            {
                HandlerInfo handler = requestsWithResponse[i];
                string ifKeyword = i == 0 ? "if" : "else if";

                sb.AppendLine($"\t\t\t{ifKeyword} (requestType == typeof({handler.RequestOrNotificationType}))");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine(
                    $"\t\t\t\tvar handler = _provider.GetRequiredService<Coordix.Interfaces.IRequestHandler<{handler.RequestOrNotificationType}, {handler.ResponseType}>>();");
                sb.AppendLine($"\t\t\t\tvar typedRequest = ({handler.RequestOrNotificationType})(object)request;");
                sb.AppendLine($"\t\t\t\tvar result = await handler.Handle(typedRequest, cancellationToken);");
                sb.AppendLine($"\t\t\t\treturn (TResponse)(object)result!;");
                sb.AppendLine("\t\t\t}");
            }

            sb.AppendLine("\t\t\telse");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
            sb.AppendLine("\t\t\t}");
        }
        else
        {
            sb.AppendLine("\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
        }

        sb.AppendLine("\t\t}");
    }

    private static void GenerateExecuteRequestHandlerWithoutResponse(StringBuilder sb, List<HandlerInfo> handlers)

    /* Unmerged change from project 'CodeGen(netstandard2.1)'
    Before:
            var requestsWithoutResponse = handlers
    After:
            List<HandlerInfo> requestsWithoutResponse = handlers
    */
    {
        List<HandlerInfo> requestsWithoutResponse = handlers
            .Where(h => h.Kind == HandlerKind.RequestWithoutResponse)
            .ToList();

        sb.AppendLine(
            "\t\tpublic async Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tvar requestType = request.GetType();");
        sb.AppendLine();

        if (requestsWithoutResponse.Count > 0)
        {
            for (int i = 0; i < requestsWithoutResponse.Count; i++)
            {
                HandlerInfo handler = requestsWithoutResponse[i];
                string ifKeyword = i == 0 ? "if" : "else if";

                sb.AppendLine($"\t\t\t{ifKeyword} (requestType == typeof({handler.RequestOrNotificationType}))");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine(
                    $"\t\t\t\tvar handler = _provider.GetRequiredService<Coordix.Interfaces.IRequestHandler<{handler.RequestOrNotificationType}>>();");
                sb.AppendLine($"\t\t\t\tvar typedRequest = ({handler.RequestOrNotificationType})(object)request;");
                sb.AppendLine($"\t\t\t\tawait handler.Handle(typedRequest, cancellationToken);");
                sb.AppendLine("\t\t\t\treturn;");
                sb.AppendLine("\t\t\t}");
            }

            sb.AppendLine("\t\t\telse");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
            sb.AppendLine("\t\t\t}");
        }
        else
        {
            sb.AppendLine("\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
        }

        sb.AppendLine("\t\t}");
    }

    private static void GenerateExecuteNotificationHandler(StringBuilder sb, List<HandlerInfo> handlers)

    /* Unmerged change from project 'CodeGen(netstandard2.1)'
    Before:
            var notifications = handlers
    After:
            List<IGrouping<string, HandlerInfo>> notifications = handlers
    */
    {
        List<IGrouping<string, HandlerInfo>> notifications = handlers
            .Where(h => h.Kind == HandlerKind.Notification)
            .GroupBy(h => h.RequestOrNotificationType)
            .ToList();

        sb.AppendLine(
            "\t\tpublic async Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)");
        sb.AppendLine("\t\t\twhere TNotification : INotification");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tvar notificationType = notification.GetType();");
        sb.AppendLine();

        if (notifications.Count > 0)
        {
            for (int i = 0; i < notifications.Count; i++)
            {
                IGrouping<string, HandlerInfo>? notificationGroup = notifications[i];
                string? notificationType = notificationGroup.Key;
                List<HandlerInfo> notificationHandlers = notificationGroup.ToList();
                string ifKeyword = i == 0 ? "if" : "else if";

                sb.AppendLine($"\t\t\t{ifKeyword} (notificationType == typeof({notificationType}))");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine($"\t\t\t\tvar typedNotification = ({notificationType})(object)notification;");
                sb.AppendLine("\t\t\t\tvar tasks = new Task[]");
                sb.AppendLine("\t\t\t\t{");

                foreach (HandlerInfo handler in notificationHandlers)
                {
                    sb.AppendLine(
                        $"\t\t\t\t\t_provider.GetRequiredService<Coordix.Interfaces.INotificationHandler<{notificationType}>>().Handle(typedNotification, cancellationToken),");
                }

                sb.AppendLine("\t\t\t\t};");
                sb.AppendLine("\t\t\t\tawait Task.WhenAll(tasks);");
                sb.AppendLine("\t\t\t\treturn;");
                sb.AppendLine("\t\t\t}");
            }

            sb.AppendLine("\t\t\telse");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\t// No handlers registered for this notification type - this is valid");
            sb.AppendLine("\t\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\t\treturn;");
            sb.AppendLine("\t\t\t}");
        }
        else
        {
            sb.AppendLine("\t\t\t// No notification handlers discovered - this is valid");
            sb.AppendLine("\t\t\tawait Task.CompletedTask;");
            sb.AppendLine("\t\t\treturn;");
        }

        sb.AppendLine("\t\t}");
    }

    private static void GenerateDynamicExecutionMethods(StringBuilder sb)
    {
        // Generate ExecuteRequestHandlerDynamic
        sb.AppendLine("\t\t/// <summary>");
        sb.AppendLine("\t\t/// Executes a request handler with a response type using runtime type information.");
        sb.AppendLine("\t\t/// This method is designed for scenarios where generic type parameters are not available at compile-time,");
        sb.AppendLine("\t\t/// such as background job processing. Delegates to the appropriate generic method.");
        sb.AppendLine("\t\t/// </summary>");
        sb.AppendLine("\t\tpublic async Task<object> ExecuteRequestHandlerDynamic(object request, Type responseType, CancellationToken cancellationToken = default)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tif (request == null) throw new ArgumentNullException(nameof(request));");
        sb.AppendLine("\t\t\tif (responseType == null) throw new ArgumentNullException(nameof(responseType));");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Use reflection once to call the generic method ExecuteRequestHandler<TResponse>");
        sb.AppendLine("\t\t\tvar requestType = request.GetType();");
        sb.AppendLine("\t\t\tvar requestInterfaceType = typeof(IRequest<>).MakeGenericType(responseType);");
        sb.AppendLine();
        sb.AppendLine("\t\t\tif (!requestInterfaceType.IsAssignableFrom(requestType))");
        sb.AppendLine("\t\t\t{");
        sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"Request type {requestType.Name} does not implement IRequest<{responseType.Name}>\");");
        sb.AppendLine("\t\t\t}");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Get the ExecuteRequestHandler<TResponse> method");
        sb.AppendLine("\t\t\tvar method = typeof(IHandlerExecutor).GetMethod(nameof(ExecuteRequestHandler), 1, new[] { requestInterfaceType, typeof(CancellationToken) });");
        sb.AppendLine("\t\t\tif (method == null)");
        sb.AppendLine("\t\t\t{");
        sb.AppendLine("\t\t\t\tmethod = typeof(IHandlerExecutor).GetMethods()");
        sb.AppendLine("\t\t\t\t\t.FirstOrDefault(m => m.Name == nameof(ExecuteRequestHandler) && m.IsGenericMethod && m.GetParameters().Length == 2);");
        sb.AppendLine("\t\t\t\tif (method == null)");
        sb.AppendLine("\t\t\t\t\tthrow new InvalidOperationException($\"ExecuteRequestHandler method not found for response type {responseType.Name}\");");
        sb.AppendLine("\t\t\t\tmethod = method.MakeGenericMethod(responseType);");
        sb.AppendLine("\t\t\t}");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Invoke the method");
        sb.AppendLine("\t\t\tvar task = (Task)method.Invoke(this, new[] { request, cancellationToken })!;");
        sb.AppendLine("\t\t\tawait task;");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Extract the result from Task<TResponse>");
        sb.AppendLine("\t\t\tvar resultProperty = task.GetType().GetProperty(\"Result\")!;");
        sb.AppendLine("\t\t\treturn resultProperty.GetValue(task)!;");
        sb.AppendLine("\t\t}");
        sb.AppendLine();

        // Generate ExecuteNotificationHandlerDynamic
        sb.AppendLine("\t\t/// <summary>");
        sb.AppendLine("\t\t/// Executes all registered notification handlers using runtime type information.");
        sb.AppendLine("\t\t/// This method is designed for scenarios where generic type parameters are not available at compile-time,");
        sb.AppendLine("\t\t/// such as background job processing. Delegates to the appropriate generic method.");
        sb.AppendLine("\t\t/// </summary>");
        sb.AppendLine("\t\tpublic async Task ExecuteNotificationHandlerDynamic(object notification, Type notificationType, CancellationToken cancellationToken = default)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tif (notification == null) throw new ArgumentNullException(nameof(notification));");
        sb.AppendLine("\t\t\tif (notificationType == null) throw new ArgumentNullException(nameof(notificationType));");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Use reflection once to call the generic method ExecuteNotificationHandler<TNotification>");
        sb.AppendLine("\t\t\tvar method = typeof(IHandlerExecutor).GetMethods()");
        sb.AppendLine("\t\t\t\t.FirstOrDefault(m => m.Name == nameof(ExecuteNotificationHandler) && m.IsGenericMethod && m.GetParameters().Length == 2);");
        sb.AppendLine();
        sb.AppendLine("\t\t\tif (method == null)");
        sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"ExecuteNotificationHandler method not found for notification type {notificationType.Name}\");");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Make the generic method with the notification type");
        sb.AppendLine("\t\t\tvar genericMethod = method.MakeGenericMethod(notificationType);");
        sb.AppendLine();
        sb.AppendLine("\t\t\t// Invoke the method");
        sb.AppendLine("\t\t\tvar task = (Task)genericMethod.Invoke(this, new[] { notification, cancellationToken })!;");
        sb.AppendLine("\t\t\tawait task;");
        sb.AppendLine("\t\t}");
    }
}
