using System.Threading;
using System.Threading.Tasks;
using CodeGenSample.Notifications;
using Coordix.Interfaces;

namespace CodeGenSample.Handlers
{
	/// <summary>
	/// Second notification handler that logs audit trail when user is created.
	/// </summary>
	public class UserCreatedAuditHandler : INotificationHandler<UserCreatedNotification>
	{
		public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
		{
			Console.WriteLine($"[UserCreatedAuditHandler] Logging audit: User {notification.UserId} created");
			return Task.CompletedTask;
		}
	}
}

