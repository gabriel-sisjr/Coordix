using BackgroundJobsSample.Notifications;
using Coordix.Interfaces;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsSample.Handlers;

public sealed class OrderCreatedInventoryHandler : INotificationHandler<OrderCreatedNotification>
{
	private readonly ILogger<OrderCreatedInventoryHandler> _logger;

	public OrderCreatedInventoryHandler(ILogger<OrderCreatedInventoryHandler> logger)
	{
		_logger = logger;
	}

	public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("📊 Updating inventory for order {OrderId}", notification.OrderId);

		// Simulate inventory update
		Task.Delay(200, cancellationToken).Wait(cancellationToken);

		_logger.LogInformation("✅ Inventory updated for order {OrderId}", notification.OrderId);
		return Task.CompletedTask;
	}
}

