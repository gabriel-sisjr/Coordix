using System.Threading;
using System.Threading.Tasks;
using Coordix.Interfaces;

namespace Coordix.Background.Interfaces
{
	/// <summary>
	/// Dispatcher for background jobs that enqueues requests and notifications to be processed asynchronously.
	/// </summary>
	public interface IBackgroundMediator
	{
		/// <summary>
		/// Enqueues a request to be processed in the background.
		/// </summary>
		/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
		/// <param name="request">The request message to enqueue.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous enqueue operation.</returns>
		Task Enqueue<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Enqueues a request without response to be processed in the background.
		/// </summary>
		/// <param name="request">The request message to enqueue.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous enqueue operation.</returns>
		Task Enqueue(IRequest request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Enqueues a notification to be processed in the background.
		/// </summary>
		/// <typeparam name="TNotification">The type of notification to enqueue.</typeparam>
		/// <param name="notification">The notification message to enqueue.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task representing the asynchronous enqueue operation.</returns>
		Task Enqueue<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification;
	}
}

