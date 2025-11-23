using System;
using System.Linq;
using System.Reflection;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coordix.CodeGen.Extensions;

public static class ServiceCollectionExtensions
{
    private const string GeneratedExecutorTypeName = "Coordix.Implementation.GeneratedHandlerExecutor";

    /// <summary>
    /// Adds Coordix services with code generation mode enabled.
    /// This method automatically registers core Coordix services (IMediator, handlers, etc.)
    /// and configures the system to use the code-generated handler executor.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated handler executor type is not found.
    /// This typically means the Coordix.CodeGen source generator hasn't run or the project hasn't been built.
    /// </exception>
    public static IServiceCollection AddCoordixWithCodeGen(this IServiceCollection services)
    {
        return AddCoordixWithCodeGen(services, Array.Empty<object>());
    }

    /// <summary>
    /// Adds Coordix services with code generation mode enabled and assembly scanning parameters.
    /// This method automatically registers core Coordix services (IMediator, handlers, etc.)
    /// and configures the system to use the code-generated handler executor.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="args">
    /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
    /// or namespace prefix strings.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated handler executor type is not found.
    /// This typically means the Coordix.CodeGen source generator hasn't run or the project hasn't been built.
    /// </exception>
    public static IServiceCollection AddCoordixWithCodeGen(
        this IServiceCollection services,
        params object[] args)
    {
        // Resolve assemblies using the same logic as core (or scan all if no args provided)
        Assembly[] assemblies = ResolveAssemblies(args);

        // Find the generated executor type in the resolved assemblies
        Type? generatedExecutorType = FindGeneratedExecutorType(assemblies);

        if (generatedExecutorType == null)
        {
            throw new InvalidOperationException(
                $"Generated HandlerExecutor type '{GeneratedExecutorTypeName}' was not found. " +
                "Make sure the Coordix.CodeGen source generator is properly installed and the project has been built.");
        }

        // Register core Coordix services with default Reflection mode first
        // We can't pass CodeGenPreferred here because Core will throw an exception
        Coordix.Extensions.ServiceCollectionExtensions.AddCoordix(services, args);

        // Replace the reflection-based executor with the generated implementation
        services.RemoveAll<IHandlerExecutor>();
        services.TryAddSingleton(typeof(IHandlerExecutor), generatedExecutorType);

        // Update CoordixOptions to reflect that we're using CodeGen mode
        services.RemoveAll<CoordixOptions>();
        services.TryAddSingleton(new CoordixOptions
        {
            HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred
        });

        return services;
    }

    /// <summary>
    /// Determines which assemblies to scan based on the provided arguments.
    /// This mirrors the logic in Coordix.Core for consistency.
    /// </summary>
    /// <param name="args">
    /// An array of arguments that may be:
    /// - No elements: returns all non-dynamic assemblies loaded in the current AppDomain
    /// - All Assembly instances: returns those assemblies directly
    /// - All string prefixes: returns assemblies whose FullName begins with any of the prefixes
    /// </param>
    /// <returns>An array of Assembly instances to scan for the generated executor.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the arguments are mixed types (neither all Assembly nor all string).
    /// </exception>
    private static Assembly[] ResolveAssemblies(object[] args)
    {
        if (args.Length == 0)
        {
            return GetAllCurrentAssemblies();
        }

        if (args.All(a => a is Assembly))
        {
            return args.Cast<Assembly>().ToArray();
        }

        if (!args.All(a => a is string))
        {
            throw new ArgumentException(
                "Invalid parameters for AddCoordixWithCodeGen(). Use: no arguments, Assembly[], or prefix strings.");
        }

        // Filter by namespace prefixes
        string[] prefixes = args.Cast<string>().ToArray();
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a =>
                !a.IsDynamic &&
                !string.IsNullOrWhiteSpace(a.FullName) &&
                prefixes.Any(p => a.FullName.StartsWith(p)))
            .ToArray();
    }

    /// <summary>
    /// Retrieves all assemblies currently loaded in the application's default AppDomain,
    /// excluding any dynamic assemblies or those without a valid FullName.
    /// </summary>
    /// <returns>
    /// An array of Assembly objects representing the filtered, non-dynamic assemblies.
    /// </returns>
    private static Assembly[] GetAllCurrentAssemblies()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
            .ToArray();
    }

    /// <summary>
    /// Searches for the generated handler executor type in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to search through.</param>
    /// <returns>
    /// The Type of the generated executor if found and it implements IHandlerExecutor; otherwise, null.
    /// </returns>
    private static Type? FindGeneratedExecutorType(Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            try
            {
                Type? type = assembly.GetType(GeneratedExecutorTypeName);
                if (type != null && typeof(IHandlerExecutor).IsAssignableFrom(type))
                {
                    return type;
                }
            }
            catch
            {
                // Skip assemblies that can't be searched (e.g., missing dependencies)
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds Coordix.Background services with CodeGen mode enabled.
    /// This method automatically registers core Coordix services with code generation,
    /// discovers handlers, and configures background job processing.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated handler executor type is not found.
    /// This typically means the Coordix.CodeGen source generator hasn't run or the project hasn't been built.
    /// </exception>
    /// <remarks>
    /// This method requires both Coordix.CodeGen and Coordix.Background packages to be installed.
    /// </remarks>
    public static IServiceCollection AddCoordixBackgroundWithCodeGen(this IServiceCollection services)
    {
        return AddCoordixBackgroundWithCodeGen(services, Array.Empty<object>());
    }

    /// <summary>
    /// Adds Coordix.Background services with CodeGen mode enabled and assembly scanning parameters.
    /// This method automatically registers core Coordix services with code generation,
    /// discovers handlers, and configures background job processing.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="args">
    /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
    /// or namespace prefix strings.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated handler executor type is not found.
    /// This typically means the Coordix.CodeGen source generator hasn't run or the project hasn't been built.
    /// </exception>
    /// <remarks>
    /// This method requires both Coordix.CodeGen and Coordix.Background packages to be installed.
    /// </remarks>
    public static IServiceCollection AddCoordixBackgroundWithCodeGen(
        this IServiceCollection services,
        params object[] args)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // 1. Register Coordix with CodeGen mode
        AddCoordixWithCodeGen(services, args);

        // 2. Register background worker components
        // This calls the internal method from Coordix.Background package
        Coordix.Background.Extensions.ServiceCollectionExtensions.RegisterBackgroundWorker(services);

        return services;
    }
}

