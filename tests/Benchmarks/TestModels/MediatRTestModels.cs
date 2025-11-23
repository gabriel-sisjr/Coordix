using MediatR;

namespace Coordix.Benchmarks.TestModels;

// MediatR-compatible request and response models
public class MediatRTestRequest : IRequest<MediatRTestResponse>
{
    public string Data { get; set; } = string.Empty;
}

public class MediatRTestResponse
{
    public string Result { get; set; } = string.Empty;
}

public class MediatRTestRequestHandler : IRequestHandler<MediatRTestRequest, MediatRTestResponse>
{
    public Task<MediatRTestResponse> Handle(MediatRTestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MediatRTestResponse { Result = $"Processed: {request.Data}" });
    }
}

// MediatR-compatible notification model
public class MediatRTestNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

// MediatR-compatible notification handlers
public class MediatRTestNotificationHandler1 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler2 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler3 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler4 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler5 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler6 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler7 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler8 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler9 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class MediatRTestNotificationHandler10 : INotificationHandler<MediatRTestNotification>
{
    public Task Handle(MediatRTestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

