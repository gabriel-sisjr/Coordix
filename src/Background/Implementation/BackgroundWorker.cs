using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coordix.Background.Implementation
{
    /// <summary>
    /// Background service that processes enqueued jobs from the channel.
    /// Uses IHandlerExecutor with zero reflection overhead - all reflection is centralized
    /// and cached within the executor itself. The BackgroundWorker simply delegates to
    /// the executor's dynamic methods which handle type resolution internally.
    /// 
    /// Each background job is processed within its own service scope, ensuring proper lifetime
    /// management for scoped services (e.g., scoped handlers, DbContext, etc.). The scope is
    /// created before processing the job and disposed after completion.
    /// </summary>
    public class BackgroundWorker : BackgroundService
    {
        private readonly ChannelReader<BackgroundJob> _channelReader;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundWorker> _logger;

        public BackgroundWorker(
            ChannelReader<BackgroundJob> channelReader,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BackgroundWorker> logger)
        {
            _channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background worker started");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (await _channelReader.WaitToReadAsync(stoppingToken))
                        {
                            while (_channelReader.TryRead(out BackgroundJob? job))
                            {
                                try
                                {
                                    await ProcessJobAsync(job, stoppingToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing background job: {MessageType}", job.MessageType.Name);
                                    // Continue processing other jobs even if one fails
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background worker is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Background worker encountered a fatal error");
                throw;
            }
        }

        /// <summary>
        /// Processes a single background job within its own service scope.
        /// Creates a new scope, resolves the handler executor from the scope,
        /// executes the job via executor's dynamic methods (zero reflection overhead),
        /// and disposes the scope when done. This ensures proper lifetime management
        /// for scoped services used by handlers.
        /// </summary>
        /// <param name="job">The background job to process.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
        {
            // Create a new service scope for this job
            // This ensures that scoped services (handlers, DbContext, etc.) are properly
            // scoped to the lifetime of this job and disposed when the job completes
            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            // Resolve the handler executor (registry) from the scope
            // The executor and all handlers it resolves will be scoped to this job
            IHandlerExecutor handlerExecutor = scope.ServiceProvider.GetService<IHandlerExecutor>();
            if (handlerExecutor == null)
            {
                _logger.LogError("IHandlerExecutor not found in service provider");
                return;
            }

            _logger.LogDebug("Processing background job: {MessageType}", job.MessageType.Name);

            try
            {
                // Execute the job using the handler executor
                // All handler resolution and execution happens within this scope
                // The executor now handles all reflection internally via its Dynamic methods
                if (job.HasResponse && job.ResponseType != null)
                {
                    // Request with response - use dynamic execution (zero reflection in BackgroundWorker)
                    await handlerExecutor.ExecuteRequestHandlerDynamic(job.Message, job.ResponseType, cancellationToken);
                }
                else if (job.Message is IRequest request)
                {
                    // Request without response - direct execution (no reflection needed)
                    await handlerExecutor.ExecuteRequestHandler(request, cancellationToken);
                }
                else if (job.Message is INotification notification)
                {
                    // Notification - use dynamic execution (zero reflection in BackgroundWorker)
                    await handlerExecutor.ExecuteNotificationHandlerDynamic(notification, job.MessageType, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Background job message is neither IRequest nor INotification: {MessageType}", job.MessageType.Name);
                }

                _logger.LogDebug("Background job processed successfully: {MessageType}", job.MessageType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background job: {MessageType}", job.MessageType.Name);
                throw; // Re-throw to be caught by the outer try-catch
            }
            // Scope is automatically disposed here via 'using', cleaning up all scoped services
        }
    }
}

