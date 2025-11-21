using Coordix.Interfaces;

namespace CodeGenSample.Notifications
{
	/// <summary>
	/// Notification published when a user is created.
	/// </summary>
	public class UserCreatedNotification : INotification
	{
		public int UserId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}

