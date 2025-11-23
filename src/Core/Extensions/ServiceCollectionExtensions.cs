using System;
using System.Linq;
using System.Reflection;
using Coordix.Implementation;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container.
        /// This is an alias for <see cref="AddCoordix(IServiceCollection)"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator and handlers will be added.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        public static IServiceCollection AddMediator(this IServiceCollection services)
                => AddCoordix(services);

        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container.
        /// This is an alias for <see cref="AddCoordix(IServiceCollection, object[])"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator and handlers will be added.
        /// </param>
        /// <param name="args">
        /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
        /// or namespace prefix strings.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        public static IServiceCollection AddMediator(this IServiceCollection services, params object[] args)
                => AddCoordix(services, args);

        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator, handler executor, and handlers will be added.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        public static IServiceCollection AddCoordix(this IServiceCollection services)
        {
            return AddCoordix(services, (Action<CoordixOptions>?)null);
        }

        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container
        /// with assembly scanning parameters (backward compatibility).
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator, handler executor, and handlers will be added.
        /// </param>
        /// <param name="args">
        /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
        /// or namespace prefix strings.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        public static IServiceCollection AddCoordix(this IServiceCollection services, params object[] args)
        {
            return AddCoordix(services, (Action<CoordixOptions>?)null, args);
        }

        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container
        /// with optional configuration.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator, handler executor, and handlers will be added.
        /// </param>
        /// <param name="configureOptions">
        /// Optional action to configure the <see cref="CoordixOptions"/>. If not provided, defaults are used
        /// (HandlerResolutionMode.Reflection).
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when <see cref="HandlerResolutionMode.CodeGenPreferred"/> is selected, as this mode
        /// is not yet implemented.
        /// </exception>
        public static IServiceCollection AddCoordix(
            this IServiceCollection services,
            Action<CoordixOptions>? configureOptions)
        {
            return AddCoordix(services, configureOptions, Array.Empty<object>());
        }

        /// <summary>
        /// Adds the Coordix mediator services and handler registrations to the dependency injection container
        /// with optional configuration and assembly scanning parameters.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator, handler executor, and handlers will be added.
        /// </param>
        /// <param name="configureOptions">
        /// Optional action to configure the <see cref="CoordixOptions"/>. If not provided, defaults are used
        /// (HandlerResolutionMode.Reflection).
        /// </param>
        /// <param name="args">
        /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
        /// or namespace prefix strings.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when <see cref="HandlerResolutionMode.CodeGenPreferred"/> is selected, as this mode
        /// is not yet implemented.
        /// </exception>
        /// <remarks>
        /// Registration order:
        /// 1. CoordixOptions - registered as singleton
        /// 2. IHandlerExecutor (registry) - registered as singleton based on HandlerResolutionMode
        /// 3. IMediator - registered as singleton, depends on IHandlerExecutor
        /// 4. Handler implementations - registered as transient, discovered via assembly scanning
        /// </remarks>
        public static IServiceCollection AddCoordix(
            this IServiceCollection services,
            Action<CoordixOptions>? configureOptions,
            params object[] args)
        {
            // Instantiate options with defaults
            CoordixOptions options = new CoordixOptions();

            // Apply configuration delegate if provided
            configureOptions?.Invoke(options);

            return AddCoordix(services, options, args);
        }


        /// <summary>
        /// Registers the core Coordix services including IHandlerExecutor (registry) and IMediator,
        /// and scans the specified assemblies for implementations of notification and request handler
        /// interfaces, registering them as transient services.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which the mediator, handler executor, and handlers will be added.
        /// </param>
        /// <param name="options">
        /// Configuration options for Coordix services.
        /// </param>
        /// <param name="args">
        /// Optional parameters to control which assemblies are scanned—either none, an array of <see cref="Assembly"/>,
        /// or namespace prefix strings.
        /// </param>
        /// <returns>
        /// The same <see cref="IServiceCollection"/> instance, to allow chaining.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when <see cref="HandlerResolutionMode.CodeGenPreferred"/> is selected, as this mode
        /// is not yet implemented.
        /// </exception>
        /// <remarks>
        /// Registration order:
        /// 1. CoordixOptions - registered as singleton
        /// 2. IHandlerExecutor (registry) - registered as singleton based on HandlerResolutionMode
        /// 3. IMediator - registered as singleton, depends on IHandlerExecutor
        /// 4. Handler implementations - registered as transient, discovered via assembly scanning
        /// </remarks>
        private static IServiceCollection AddCoordix(
            this IServiceCollection services,
            CoordixOptions options,
            params object[] args)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Assembly[] assemblies = ResolveAssemblies(args);

            // Register CoordixOptions as singleton so it can be injected
            services.AddSingleton(options);

            // Register HandlerExecutor (registry) based on the selected resolution mode
            // This allows different implementations to be plugged in based on the mode.
            // When Coordix.CodeGen package is added, it will provide its own registration
            // method that registers a code-generated implementation for CodeGenPreferred mode.
            switch (options.HandlerResolutionMode)
            {
                case HandlerResolutionMode.Reflection:
                    // Register reflection-based handler executor - this centralizes all handler execution logic
                    // including reflection, caching, and invocation. Registered as singleton to share caches.
                    RegisterReflectionBasedExecutor(services);
                    break;

                case HandlerResolutionMode.CodeGenPreferred:
                    // Code generation mode requires the Coordix.CodeGen package to be installed
                    // and its registration method to be called. This ensures explicit opt-in
                    // rather than silent fallback behavior.
                    throw new InvalidOperationException(
                        $"HandlerResolutionMode.{nameof(HandlerResolutionMode.CodeGenPreferred)} requires the Coordix.CodeGen package. " +
                        $"Install the package and call the CodeGen registration method, or use {nameof(HandlerResolutionMode.Reflection)} mode.");

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(options),
                        $"Unknown HandlerResolutionMode: {options.HandlerResolutionMode}");
            }

            // Register Mediator - depends on IHandlerExecutor via constructor injection
            services.AddSingleton<IMediator, Mediator>();

            // Register discovered handlers as transient services
            RegisterHandlers(services, assemblies, typeof(INotificationHandler<>));
            RegisterHandlers(services, assemblies, typeof(IRequestHandler<,>));
            RegisterHandlers(services, assemblies, typeof(IRequestHandler<>));

            return services;
        }

        /// <summary>
        /// Determines which assemblies to scan based on the provided arguments.
        /// </summary>
        /// <param name="args">
        /// An array of arguments that may be:
        /// <list type="bullet">
        ///   <item>No elements: returns all non-dynamic assemblies loaded in the current AppDomain.</item>
        ///   <item>All <see cref="Assembly"/> instances: returns those assemblies directly.</item>
        ///   <item>All <see cref="string"/> prefixes: returns assemblies whose FullName begins with any of the prefixes.</item>
        /// </list>
        /// </param>
        /// <returns>
        /// An array of <see cref="Assembly"/> instances to scan for handler implementations.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the arguments are mixed types (neither all <see cref="Assembly"/> nor all <see cref="string"/>).
        /// </exception>
        private static Assembly[] ResolveAssemblies(object[] args)
        {
            if (args.Length == 0)
            {
                return GetAllCurrentAssemblies();
            }

            // Return all informed, same behavior as args.Length == 0.
            if (args.All(a => a is Assembly))
            {
                return args.Cast<Assembly>().ToArray();
            }

            if (!args.All(a => a is string))
            {
                throw new ArgumentException(
                        "Invalid parameters for AddMediator() or AddCoordix(). Use: no arguments, Assembly[], or prefix strings.");
            }

            // Return filtered by namespace
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
        /// Retrieves all assemblies currently loaded in the application's default <see cref="AppDomain"/>,
        /// excluding any dynamic assemblies or those without a valid <see cref="Assembly.FullName"/>.
        /// </summary>
        /// <returns>
        /// An array of <see cref="Assembly"/> objects representing the filtered, non-dynamic assemblies
        /// loaded into <see cref="AppDomain.CurrentDomain"/>.
        /// </returns>
        private static Assembly[] GetAllCurrentAssemblies()
        {
            return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                    .ToArray();
        }

        /// <summary>
        /// Registers the reflection-based handler executor implementation.
        /// This method is called when HandlerResolutionMode.Reflection is selected.
        /// When Coordix.CodeGen is added, it will provide a similar method for CodeGenPreferred mode.
        /// </summary>
        /// <param name="services">The service collection to register the executor in.</param>
        private static void RegisterReflectionBasedExecutor(IServiceCollection services)
        {
            // Register reflection-based handler executor - this centralizes all handler execution logic
            // including reflection, caching, and invocation. Registered as singleton to share caches.
            services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        }

        /// <summary>
        /// Scans the provided assemblies for all non-abstract, concrete classes that implement
        /// the specified generic handler interface, and registers each implementation with the
        /// dependency injection container as a transient service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to which discovered handler implementations
        /// will be added.
        /// </param>
        /// <param name="assemblies">
        /// An array of <see cref="Assembly"/> instances to scan for types implementing
        /// the handler interface.
        /// </param>
        /// <param name="handlerInterface">
        /// The open generic interface type (e.g. <c>typeof(IRequestHandler&lt;, &gt;)</c>)
        /// that handler classes must implement to be registered.
        /// </param>
        private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Type handlerInterface)
        {
            System.Collections.Generic.List<Type> types = assemblies
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // If some types can't be loaded (e.g., missing dependencies), use the successfully loaded ones
                            return ex.Types.Where(t => t != null)!;
                        }
                        catch
                        {
                            // Skip assemblies that can't be loaded
                            return Array.Empty<Type>();
                        }
                    })
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .ToList();

            types.ForEach(type =>
            {
                System.Collections.Generic.List<Type> interfaces = type.GetInterfaces()
                                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                                    .ToList();

                interfaces.ForEach(interFace => services.AddTransient(interFace, type));
            });
        }
    }
}
