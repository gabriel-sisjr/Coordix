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
		var candidateClasses = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => IsCandidateClass(node),
				transform: static (ctx, _) => GetSemanticTarget(ctx))
			.Where(static m => m is not null);

		// Combine with compilation to access symbol information
		var compilationAndClasses = context.CompilationProvider.Combine(candidateClasses.Collect());

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
		var classDecl = (ClassDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

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
		if (namedType.IsGenericType && !namedType.IsUnboundGenericType && namedType.TypeArguments.Any(t => t.Kind == SymbolKind.TypeParameter))
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
		foreach (var iface in type.AllInterfaces)
		{
			if (!iface.IsGenericType)
			{
				continue;
			}

			var originalDef = iface.OriginalDefinition.ToDisplayString();

			if (originalDef == IRequestHandlerWithResponseFullName ||
				originalDef == IRequestHandlerWithoutResponseFullName ||
				originalDef == INotificationHandlerFullName)
			{
				return true;
			}
		}

		return false;
	}

	private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> classes, SourceProductionContext context)
	{
		if (classes.IsDefaultOrEmpty)
		{
			return;
		}

		var handlers = new List<HandlerInfo>();

		foreach (var handlerSymbol in classes)
		{
			if (handlerSymbol is null)
			{
				continue;
			}

			foreach (var iface in handlerSymbol.AllInterfaces)
			{
				if (!iface.IsGenericType)
				{
					continue;
				}

				var originalDef = iface.OriginalDefinition.ToDisplayString();

				if (originalDef == IRequestHandlerWithResponseFullName)
				{
					// IRequestHandler<TRequest, TResponse>
					var requestType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
					var responseType = iface.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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
					var requestType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

					handlers.Add(new HandlerInfo(
						handlerSymbol.Name,
						handlerSymbol.ContainingNamespace.ToDisplayString(),
						HandlerKind.RequestWithoutResponse,
						requestType));
				}
				else if (originalDef == INotificationHandlerFullName)
				{
					// INotificationHandler<TNotification>
					var notificationType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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
		var sourceText = GenerateHandlerExecutor(handlers);
		context.AddSource("GeneratedHandlerExecutor.g.cs", SourceText.From(sourceText, Encoding.UTF8));
	}

	private static string GenerateHandlerExecutor(List<HandlerInfo> handlers)
	{
		var sb = new StringBuilder();

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
		var namespaces = handlers
			.Select(h => h.HandlerNamespace)
			.Distinct()
			.OrderBy(ns => ns);

		foreach (var ns in namespaces)
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

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		return sb.ToString();
	}

	private static void GenerateExecuteRequestHandlerWithResponse(StringBuilder sb, List<HandlerInfo> handlers)
	{
		var requestsWithResponse = handlers
			.Where(h => h.Kind == HandlerKind.RequestWithResponse)
			.ToList();

		sb.AppendLine("\t\tpublic async Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar requestType = request.GetType();");
		sb.AppendLine();

		if (requestsWithResponse.Count > 0)
		{
			for (int i = 0; i < requestsWithResponse.Count; i++)
			{
				var handler = requestsWithResponse[i];
				var ifKeyword = i == 0 ? "if" : "else if";

				sb.AppendLine($"\t\t\t{ifKeyword} (requestType == typeof({handler.RequestOrNotificationType}))");
				sb.AppendLine("\t\t\t{");
				sb.AppendLine($"\t\t\t\tvar handler = _provider.GetRequiredService<Coordix.Interfaces.IRequestHandler<{handler.RequestOrNotificationType}, {handler.ResponseType}>>();");
				sb.AppendLine($"\t\t\t\tvar typedRequest = ({handler.RequestOrNotificationType})request;");
				sb.AppendLine($"\t\t\t\tvar result = await handler.Handle(typedRequest, cancellationToken);");
				sb.AppendLine($"\t\t\t\treturn (TResponse)(object)result!;");
				sb.AppendLine("\t\t\t}");
			}

			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
			sb.AppendLine("\t\t\t}");
		}
		else
		{
			sb.AppendLine("\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
		}

		sb.AppendLine("\t\t}");
	}

	private static void GenerateExecuteRequestHandlerWithoutResponse(StringBuilder sb, List<HandlerInfo> handlers)
	{
		var requestsWithoutResponse = handlers
			.Where(h => h.Kind == HandlerKind.RequestWithoutResponse)
			.ToList();

		sb.AppendLine("\t\tpublic async Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar requestType = request.GetType();");
		sb.AppendLine();

		if (requestsWithoutResponse.Count > 0)
		{
			for (int i = 0; i < requestsWithoutResponse.Count; i++)
			{
				var handler = requestsWithoutResponse[i];
				var ifKeyword = i == 0 ? "if" : "else if";

				sb.AppendLine($"\t\t\t{ifKeyword} (requestType == typeof({handler.RequestOrNotificationType}))");
				sb.AppendLine("\t\t\t{");
				sb.AppendLine($"\t\t\t\tvar handler = _provider.GetRequiredService<Coordix.Interfaces.IRequestHandler<{handler.RequestOrNotificationType}>>();");
				sb.AppendLine($"\t\t\t\tvar typedRequest = ({handler.RequestOrNotificationType})request;");
				sb.AppendLine($"\t\t\t\tawait handler.Handle(typedRequest, cancellationToken);");
				sb.AppendLine("\t\t\t\treturn;");
				sb.AppendLine("\t\t\t}");
			}

			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
			sb.AppendLine("\t\t\t}");
		}
		else
		{
			sb.AppendLine("\t\t\tthrow new InvalidOperationException($\"Handler not found for {requestType.Name}\");");
		}

		sb.AppendLine("\t\t}");
	}

	private static void GenerateExecuteNotificationHandler(StringBuilder sb, List<HandlerInfo> handlers)
	{
		var notifications = handlers
			.Where(h => h.Kind == HandlerKind.Notification)
			.GroupBy(h => h.RequestOrNotificationType)
			.ToList();

		sb.AppendLine("\t\tpublic async Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)");
		sb.AppendLine("\t\t\twhere TNotification : INotification");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar notificationType = notification.GetType();");
		sb.AppendLine();

		if (notifications.Count > 0)
		{
			for (int i = 0; i < notifications.Count; i++)
			{
				var notificationGroup = notifications[i];
				var notificationType = notificationGroup.Key;
				var notificationHandlers = notificationGroup.ToList();
				var ifKeyword = i == 0 ? "if" : "else if";

				sb.AppendLine($"\t\t\t{ifKeyword} (notificationType == typeof({notificationType}))");
				sb.AppendLine("\t\t\t{");
				sb.AppendLine($"\t\t\t\tvar typedNotification = ({notificationType})(object)notification;");
				sb.AppendLine("\t\t\t\tvar tasks = new Task[]");
				sb.AppendLine("\t\t\t\t{");

				foreach (var handler in notificationHandlers)
				{
					sb.AppendLine($"\t\t\t\t\t_provider.GetRequiredService<Coordix.Interfaces.INotificationHandler<{notificationType}>>().Handle(typedNotification, cancellationToken),");
				}

				sb.AppendLine("\t\t\t\t};");
				sb.AppendLine("\t\t\t\tawait Task.WhenAll(tasks);");
				sb.AppendLine("\t\t\t\treturn;");
				sb.AppendLine("\t\t\t}");
			}

			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// No handlers registered for this notification type - this is valid");
			sb.AppendLine("\t\t\t\treturn;");
			sb.AppendLine("\t\t\t}");
		}
		else
		{
			sb.AppendLine("\t\t\t// No notification handlers discovered - this is valid");
			sb.AppendLine("\t\t\treturn;");
		}

		sb.AppendLine("\t\t}");
	}
}
