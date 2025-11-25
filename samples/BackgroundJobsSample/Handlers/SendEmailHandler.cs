using BackgroundJobsSample.Requests;
using Coordix.Interfaces;
using Microsoft.Extensions.Logging;

namespace BackgroundJobsSample.Handlers;

public sealed class SendEmailHandler : IRequestHandler<SendEmailRequest>
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

