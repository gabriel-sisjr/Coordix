using Coordix.Interfaces;

namespace Coordix.Benchmarks.TestModels;

public class TestNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

public class TestNotificationHandler1 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        // Simulate some work
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler3 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler4 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler5 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler6 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler7 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler8 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler9 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler10 : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.Message.Length;
        return Task.CompletedTask;
    }
}

