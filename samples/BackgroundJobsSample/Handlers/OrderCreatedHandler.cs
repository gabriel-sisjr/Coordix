using BackgroundJobsSample.Notifications;
using Coordix.Interfaces;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsSample.Handlers;

public sealed class OrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
{
	private readonly ILogger<OrderCreatedHandler> _logger;

	public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
	{
		_logger = logger;
	}

	public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("📦 Order {OrderId} created for customer {CustomerId} with total {Total}",
			notification.OrderId, notification.CustomerId, notification.Total);

		// Simulate order processing
		Task.Delay(300, cancellationToken).Wait(cancellationToken);

		_logger.LogInformation("✅ Order {OrderId} processed successfully", notification.OrderId);
		return Task.CompletedTask;
	}
}

