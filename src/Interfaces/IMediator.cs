using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coordix.Interfaces
{
    public interface IMediator
    {
        /// <summary>
        /// Sends a request to a single handler and returns its response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the handler's response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request to a single handler without expecting a response.
        /// </summary>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
        Task Send(IRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
        /// <param name="notification">The notification message to publish.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous publish operation.</returns>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
