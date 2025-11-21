using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Coordix.CodeGen.Tests;

public class CoordixSourceGeneratorTests
{
	[Fact]
	public async Task Generator_WithRequestHandlerWithResponse_GeneratesExecutorCode()
	{
		// Arrange
		var source = @"
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

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.NotNull(generatedSource);
		Assert.Contains("class GeneratedHandlerExecutor", generatedSource);
		Assert.Contains("ExecuteRequestHandler<TResponse>", generatedSource);
		Assert.Contains("typeof(global::TestApp.TestRequest)", generatedSource);
		Assert.Contains("Coordix.Interfaces.IRequestHandler<global::TestApp.TestRequest, string>", generatedSource);
	}

	[Fact]
	public async Task Generator_WithRequestHandlerWithoutResponse_GeneratesExecutorCode()
	{
		// Arrange
		var source = @"
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace TestApp
{
	public class TestCommand : IRequest { }

	public class TestCommandHandler : IRequestHandler<TestCommand>
	{
		public Task Handle(TestCommand request, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}";

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.NotNull(generatedSource);
		Assert.Contains("ExecuteRequestHandler(IRequest request", generatedSource);
		Assert.Contains("typeof(global::TestApp.TestCommand)", generatedSource);
		Assert.Contains("Coordix.Interfaces.IRequestHandler<global::TestApp.TestCommand>", generatedSource);
	}

	[Fact]
	public async Task Generator_WithNotificationHandler_GeneratesExecutorCode()
	{
		// Arrange
		var source = @"
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace TestApp
{
	public class TestNotification : INotification { }

	public class TestNotificationHandler : INotificationHandler<TestNotification>
	{
		public Task Handle(TestNotification notification, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}";

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.NotNull(generatedSource);
		Assert.Contains("ExecuteNotificationHandler<TNotification>", generatedSource);
		Assert.Contains("typeof(global::TestApp.TestNotification)", generatedSource);
		Assert.Contains("Coordix.Interfaces.INotificationHandler<global::TestApp.TestNotification>", generatedSource);
	}

	[Fact]
	public async Task Generator_WithMultipleHandlers_GeneratesAllBranches()
	{
		// Arrange
		var source = @"
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace TestApp
{
	public class Request1 : IRequest<int> { }
	public class Request2 : IRequest<string> { }
	
	public class Handler1 : IRequestHandler<Request1, int>
	{
		public Task<int> Handle(Request1 request, CancellationToken cancellationToken)
		{
			return Task.FromResult(42);
		}
	}
	
	public class Handler2 : IRequestHandler<Request2, string>
	{
		public Task<string> Handle(Request2 request, CancellationToken cancellationToken)
		{
			return Task.FromResult(""test"");
		}
	}
}";

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.NotNull(generatedSource);
		Assert.Contains("typeof(global::TestApp.Request1)", generatedSource);
		Assert.Contains("typeof(global::TestApp.Request2)", generatedSource);
		Assert.Contains("Coordix.Interfaces.IRequestHandler<global::TestApp.Request1, int>", generatedSource);
		Assert.Contains("Coordix.Interfaces.IRequestHandler<global::TestApp.Request2, string>", generatedSource);
	}

	[Fact]
	public async Task Generator_WithNoHandlers_GeneratesEmptyExecutor()
	{
		// Arrange
		var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
	public class EmptyClass { }
}";

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		// When no handlers are found, generator should not produce output
		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.Null(generatedSource);
	}

	[Fact]
	public async Task Generator_WithAbstractHandler_DoesNotGenerate()
	{
		// Arrange
		var source = @"
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace TestApp
{
	public class TestRequest : IRequest<string> { }

	public abstract class AbstractHandler : IRequestHandler<TestRequest, string>
	{
		public abstract Task<string> Handle(TestRequest request, CancellationToken cancellationToken);
	}
}";

		// Act
		var (compilation, diagnostics) = await RunGenerator(source);

		// Assert
		Assert.Empty(diagnostics);

		// Abstract handlers should be ignored
		var generatedSource = GetGeneratedSource(compilation, "GeneratedHandlerExecutor.g.cs");
		Assert.Null(generatedSource);
	}

	private async Task<(Compilation, Diagnostic[])> RunGenerator(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);

		// Reference assemblies needed for compilation
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(System.IServiceProvider).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Coordix.Interfaces.IRequest).Assembly.Location),
		};

		// Create compilation
		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		// Create generator instance
		var generator = new CoordixSourceGenerator();

		// Create driver and run generator
		var driver = CSharpGeneratorDriver.Create(generator);
		driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var diagnostics);

		// Filter out warnings we don't care about in tests
		var relevantDiagnostics = diagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		return await Task.FromResult((outputCompilation, relevantDiagnostics));
	}

	private string? GetGeneratedSource(Compilation compilation, string fileName)
	{
		var generatedTree = compilation.SyntaxTrees
			.FirstOrDefault(t => t.FilePath.EndsWith(fileName));

		return generatedTree?.ToString();
	}
}

