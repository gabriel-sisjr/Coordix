using System.Threading;
using System.Threading.Tasks;

namespace Coordix.Interfaces
{
	/// <summary>
	/// Centralized handler execution abstraction that handles handler lookup, caching, and invocation.
	/// This interface ensures that all handler execution logic is centralized in a single place,
	/// eliminating scattered reflection throughout the codebase.
	/// </summary>
	public interface IHandlerExecutor
	{
		/// <summary>
		/// Executes a request handler with a response type.
		/// Performs handler lookup via DI, uses cached delegates for performance, and invokes the handler.
		/// </summary>
		/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
		/// <param name="request">The request message to execute.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous operation, containing the handler's response.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
		Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Executes a request handler without a response type.
		/// Performs handler lookup via DI, uses cached delegates for performance, and invokes the handler.
		/// </summary>
		/// <param name="request">The request message to execute.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no handler is found for the request type.</exception>
		Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Executes all registered notification handlers for the given notification.
		/// Performs handler lookup via DI, uses cached delegates for performance, and invokes all handlers in parallel.
		/// </summary>
		/// <typeparam name="TNotification">The type of notification to execute.</typeparam>
		/// <param name="notification">The notification message to execute.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification;
	}
}

