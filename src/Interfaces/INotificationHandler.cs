using System.Threading;
using System.Threading.Tasks;

namespace Coordix.Interfaces
{
    public interface INotificationHandler<TNotification> where TNotification : INotification
    {
        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
        /// <param name="notification">The notification message to publish.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous publish operation.</returns>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
