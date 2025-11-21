using System;
using System.Threading.Channels;
using Coordix.Background.Implementation;
using Coordix.Background.Interfaces;
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
        /// Adds Coordix.Background services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCoordixBackground(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

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

            return services;
        }
    }
}

