using Coordix.Implementation;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Coordix.Extensions
{
    public static class ServiceCollectionExtensions
    {
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
        /// Registers the core Mediator implementation, and scans the specified assemblies for
        /// implementations of notification and request handler interfaces, registering them as transient services.
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
        public static IServiceCollection AddCoordix(this IServiceCollection services, params object[] args)
        {
            var assemblies = ResolveAssemblies(args);

            services.AddSingleton<IMediator, Mediator>();

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
            if (args.Length == 0) return GetAllCurrentAssemblies();

            // Return all informed, same behavior as args.Length == 0.
            if (args.All(a => a is Assembly))
                return args.Cast<Assembly>().ToArray();

            if (!args.All(a => a is string))
                throw new ArgumentException(
                    "Invalid parameters for AddMediator() or AddCoordix(). Use: no arguments, Assembly[], or prefix strings.");

            // Return filtered by namespace
            var prefixes = args.Cast<string>().ToArray();
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
            var types = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            types.ForEach(type =>
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                    .ToList();

                interfaces.ForEach(interFace => services.AddTransient(interFace, type));
            });
        }
    }
}
