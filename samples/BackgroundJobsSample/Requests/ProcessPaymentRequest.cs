using Coordix.Interfaces;

namespace BackgroundJobsSample.Requests;

public sealed class ProcessPaymentRequest : IRequest<string>
{
	public decimal Amount { get; set; }
	public string CustomerId { get; set; } = string.Empty;
}

