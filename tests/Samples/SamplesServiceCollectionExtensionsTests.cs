using Coordix.Interfaces;

namespace Coordix.Tests.Samples;

public record PongResponse;
public record PingRequest : IRequest<PongResponse>;

public class PingRequestHandler : IRequestHandler<PingRequest, PongResponse>
{
    public Task<PongResponse> Handle(PingRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new PongResponse());
}

public record TestNotificationServiceCollection : INotification;
public class TestNotificationHandler : INotificationHandler<TestNotificationServiceCollection>
{
    public Task Handle(TestNotificationServiceCollection notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
