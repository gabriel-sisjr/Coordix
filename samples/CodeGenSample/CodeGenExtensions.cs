using System;
using System.Linq;
using Coordix;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeGenSample
{
	/// <summary>
	/// Extension methods for integrating Coordix CodeGen with dependency injection.
	/// This is a sample implementation - production code would package this in Coordix.CodeGen.
	/// </summary>
	public static class CodeGenExtensions
	{
		private const string GeneratedExecutorTypeName = "Coordix.Implementation.GeneratedHandlerExecutor";

		public static IServiceCollection AddCoordixWithCodeGen(
			this IServiceCollection services,
			Action<CoordixOptions>? configureOptions = null,
			params object[] args)
		{
			// Find the generated HandlerExecutor type
			var generatedExecutorType = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetType(GeneratedExecutorTypeName))
				.FirstOrDefault(t => t != null && typeof(IHandlerExecutor).IsAssignableFrom(t));

			if (generatedExecutorType == null)
			{
				throw new InvalidOperationException(
					$"Generated HandlerExecutor type '{GeneratedExecutorTypeName}' was not found. " +
					"Make sure the Coordix.CodeGen source generator is properly installed and the project has been built.");
			}

			// Register Core services with Reflection mode first
			Coordix.Extensions.ServiceCollectionExtensions.AddCoordix(
				services,
				options =>
				{
					options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
					configureOptions?.Invoke(options);
				},
				args);

			// Replace with generated executor
			services.RemoveAll<IHandlerExecutor>();
			services.AddSingleton(typeof(IHandlerExecutor), generatedExecutorType);

			// Update options to reflect CodeGenPreferred
			services.RemoveAll<CoordixOptions>();
			var finalOptions = new CoordixOptions
			{
				HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred
			};
			configureOptions?.Invoke(finalOptions);
			services.AddSingleton(finalOptions);

			return services;
		}
	}
}

