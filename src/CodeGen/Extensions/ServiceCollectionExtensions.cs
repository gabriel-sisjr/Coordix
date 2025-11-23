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
        return AddCoordixWithCodeGen(services, configureOptions: null);
    }

    /// <summary>
    /// Adds Coordix services with code generation mode enabled and optional configuration.
    /// This method automatically registers core Coordix services (IMediator, handlers, etc.)
    /// and configures the system to use the code-generated handler executor.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure additional Coordix options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated handler executor type is not found.
    /// This typically means the Coordix.CodeGen source generator hasn't run or the project hasn't been built.
    /// </exception>
    public static IServiceCollection AddCoordixWithCodeGen(
        this IServiceCollection services,
        Action<CoordixOptions>? configureOptions)
    {
        return AddCoordixWithCodeGen(services, configureOptions, Array.Empty<object>());
    }

    /// <summary>
    /// Adds Coordix services with code generation mode enabled, optional configuration,
    /// and assembly scanning parameters.
    /// This method automatically registers core Coordix services (IMediator, handlers, etc.)
    /// and configures the system to use the code-generated handler executor.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure additional Coordix options.</param>
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
        Action<CoordixOptions>? configureOptions,
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

        // Register core Coordix services with CodeGenPreferred mode
        // The HandlerResolutionMode is set first, then user options are applied
        Coordix.Extensions.ServiceCollectionExtensions.AddCoordix(
            services,
            options =>
            {
                // Set CodeGenPreferred mode first
                options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;

                // Allow user to configure other options (but not override the mode)
                configureOptions?.Invoke(options);

                // Ensure mode stays as CodeGenPreferred
                options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
            },
            args);

        // Replace the IHandlerExecutor with the generated implementation
        services.RemoveAll<IHandlerExecutor>();
        services.TryAddSingleton(typeof(IHandlerExecutor), generatedExecutorType);

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
}

