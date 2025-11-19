using Coordix.Background.Extensions;
using Coordix.Background.Interfaces;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsSample;

// Example requests and notifications
public class SendEmailRequest : IRequest
{
	public string To { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
}

public class ProcessPaymentRequest : IRequest<string>
{
	public decimal Amount { get; set; }
	public string CustomerId { get; set; } = string.Empty;
}

public class OrderCreatedNotification : INotification
{
	public string OrderId { get; set; } = string.Empty;
	public string CustomerId { get; set; } = string.Empty;
	public decimal Total { get; set; }
}

// Handlers
public class SendEmailHandler : IRequestHandler<SendEmailRequest>
{
	private readonly ILogger<SendEmailHandler> _logger;

	public SendEmailHandler(ILogger<SendEmailHandler> logger)
	{
		_logger = logger;
	}

	public Task Handle(SendEmailRequest request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("📧 Sending email to {To} with subject: {Subject}", request.To, request.Subject);
		_logger.LogInformation("   Body: {Body}", request.Body);

		// Simulate email sending
		Task.Delay(500, cancellationToken).Wait(cancellationToken);

		_logger.LogInformation("✅ Email sent successfully to {To}", request.To);
		return Task.CompletedTask;
	}
}

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentRequest, string>
{
	private readonly ILogger<ProcessPaymentHandler> _logger;

	public ProcessPaymentHandler(ILogger<ProcessPaymentHandler> logger)
	{
		_logger = logger;
	}

	public Task<string> Handle(ProcessPaymentRequest request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("💳 Processing payment of {Amount} for customer {CustomerId}",
			request.Amount, request.CustomerId);

		// Simulate payment processing
		Task.Delay(1000, cancellationToken).Wait(cancellationToken);

		var transactionId = $"TXN-{Guid.NewGuid():N}";
		_logger.LogInformation("✅ Payment processed successfully. Transaction ID: {TransactionId}", transactionId);

		return Task.FromResult(transactionId);
	}
}

public class OrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
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

public class OrderCreatedInventoryHandler : INotificationHandler<OrderCreatedNotification>
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

// Main program
public class Program
{
	public static async Task Main(string[] args)
	{
		Console.WriteLine("🚀 Coordix.Background Sample");
		Console.WriteLine("============================\n");

		// Create host with Coordix and Coordix.Background
		var host = Host.CreateDefaultBuilder(args)
			.ConfigureServices(services =>
			{
				// Add Coordix mediator
				services.AddCoordix();

				// Add Coordix.Background for fire-and-forget background jobs
				services.AddCoordixBackground();

				// Register handlers
				services.AddScoped<IRequestHandler<SendEmailRequest>, SendEmailHandler>();
				services.AddScoped<IRequestHandler<ProcessPaymentRequest, string>, ProcessPaymentHandler>();
				services.AddScoped<INotificationHandler<OrderCreatedNotification>, OrderCreatedHandler>();
				services.AddScoped<INotificationHandler<OrderCreatedNotification>, OrderCreatedInventoryHandler>();
			})
			.Build();

		// Get background mediator
		var backgroundMediator = host.Services.GetRequiredService<IBackgroundMediator>();
		var logger = host.Services.GetRequiredService<ILogger<Program>>();

		// Start the host (this starts the background worker)
		await host.StartAsync();

		logger.LogInformation("Background worker started. You can now enqueue jobs.\n");

		// Example 1: Enqueue a simple request (fire-and-forget)
		Console.WriteLine("Example 1: Enqueueing email request in background...");
		await backgroundMediator.Enqueue(new SendEmailRequest
		{
			To = "customer@example.com",
			Subject = "Welcome!",
			Body = "Thank you for joining us!"
		});
		Console.WriteLine("✅ Email request enqueued (will be processed in background)\n");

		// Example 2: Enqueue a request with response (fire-and-forget, response is ignored)
		Console.WriteLine("Example 2: Enqueueing payment processing in background...");
		await backgroundMediator.Enqueue<string>(new ProcessPaymentRequest
		{
			Amount = 99.99m,
			CustomerId = "CUST-123"
		});
		Console.WriteLine("✅ Payment request enqueued (will be processed in background)\n");

		// Example 3: Enqueue a notification (multiple handlers will be called)
		Console.WriteLine("Example 3: Enqueueing order created notification in background...");
		await backgroundMediator.Enqueue(new OrderCreatedNotification
		{
			OrderId = "ORD-456",
			CustomerId = "CUST-123",
			Total = 199.99m
		});
		Console.WriteLine("✅ Order notification enqueued (multiple handlers will process it in background)\n");

		// Wait a bit for background processing
		Console.WriteLine("Waiting for background jobs to complete...\n");
		await Task.Delay(3000);

		Console.WriteLine("\n✨ All background jobs have been processed!");
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();

		// Stop the host
		await host.StopAsync();
	}
}

