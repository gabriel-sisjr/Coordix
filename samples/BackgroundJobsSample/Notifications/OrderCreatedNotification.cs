using Coordix.Interfaces;

namespace BackgroundJobsSample.Notifications;

public sealed class OrderCreatedNotification : INotification
{
	public string OrderId { get; set; } = string.Empty;
	public string CustomerId { get; set; } = string.Empty;
	public decimal Total { get; set; }
}

