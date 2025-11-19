using Coordix.Interfaces;

namespace SimpleSample;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public async Task<string> Handle(Ping request, CancellationToken cancellationToken)
        => await Task.FromResult($"Pong: {request.Message}");
}
