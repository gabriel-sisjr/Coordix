using Coordix.Interfaces;

namespace SimpleSample;

public sealed class Ping : IRequest<string>
{
    public string Message { get; set; } = "Ping!";
}
