using BackgroundJobsSample.Requests;
using Coordix.Interfaces;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsSample.Handlers;

public sealed class ProcessPaymentHandler : IRequestHandler<ProcessPaymentRequest, string>
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

