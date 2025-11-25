using System;
using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace Coordix.Implementation
{
    /// <summary>
    /// A mediator that delegates handler execution to IHandlerExecutor (the registry).
    /// This implementation is simplified to only discover the request/notification type
    /// and delegate all handler resolution, caching, and invocation to the registry.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IHandlerExecutor _handlerExecutor;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class using the given handler executor.
        /// </summary>
        /// <param name="handlerExecutor">The handler executor (registry) used to execute handlers.</param>
        public Mediator(IHandlerExecutor handlerExecutor)
        {
            _handlerExecutor = handlerExecutor ?? throw new ArgumentNullException(nameof(handlerExecutor));
        }

        /// <summary>
        /// Sends a request to a single handler and returns its response.
        /// Discovers the request type and delegates execution to the registry.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the handler's response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            // Discover request type and delegate to registry
            return _handlerExecutor.ExecuteRequestHandler(request, cancellationToken);
        }

        /// <summary>
        /// Sends a request to a single handler without expecting a response.
        /// Discovers the request type and delegates execution to the registry.
        /// </summary>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            // Discover request type and delegate to registry
            return _handlerExecutor.ExecuteRequestHandler(request, cancellationToken);
        }

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// Discovers the notification type and delegates execution to the registry.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
        /// <param name="notification">The notification message to publish.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous publish operation.</returns>
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            // Discover notification type and delegate to registry
            return _handlerExecutor.ExecuteNotificationHandler(notification, cancellationToken);
        }
    }
}
