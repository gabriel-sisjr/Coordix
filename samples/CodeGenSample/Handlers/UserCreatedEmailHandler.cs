using System.Threading;
using System.Threading.Tasks;
using CodeGenSample.Notifications;
using Coordix.Interfaces;

namespace CodeGenSample.Handlers
{
	/// <summary>
	/// First notification handler that sends welcome email when user is created.
	/// </summary>
	public class UserCreatedEmailHandler : INotificationHandler<UserCreatedNotification>
	{
		public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
		{
			Console.WriteLine($"[UserCreatedEmailHandler] Sending welcome email to {notification.Email}");
			return Task.CompletedTask;
		}
	}
}

