using System;
using System.Threading.Channels;
using Coordix.Background.Implementation;
using Coordix.Background.Interfaces;
using Coordix.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Coordix.Background.Extensions
{
    /// <summary>
    /// Extension methods for adding Coordix.Background services to the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Coordix.Background services to the service collection with Reflection mode.
        /// This method automatically registers the core Coordix services (IMediator, IHandlerExecutor, etc.)
        /// so there is no need to call AddCoordix() explicitly.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCoordixBackground(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register core Coordix services first (Reflection mode)
            Coordix.Extensions.ServiceCollectionExtensions.AddCoordix(services);

            // Register background worker components
            RegisterBackgroundWorker(services);

            return services;
        }

        /// <summary>
        /// Registers the background worker components (channel, mediator, and hosted service).
        /// This is internal so it can be used by Coordix.CodeGen package to create
        /// AddCoordixBackgroundWithCodeGen extension method.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        internal static void RegisterBackgroundWorker(IServiceCollection services)
        {
            // Create an unbounded channel for background jobs
            Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Register the channel writer and reader as singletons
            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);

            // Register the background mediator
            services.AddSingleton<IBackgroundMediator, BackgroundMediator>();

            // Register the background worker as a hosted service
            services.AddHostedService<BackgroundWorker>();
        }
    }
}

