using Wolverine;

namespace Coordix.Benchmarks.TestModels;

// Wolverine-compatible request and response models
// Wolverine uses IMessage for requests that return responses
public class WolverineTestRequest : IMessage
{
    public string Data { get; set; } = string.Empty;
}

public class WolverineTestResponse
{
    public string Result { get; set; } = string.Empty;
}

// Wolverine handler for request/response
// Wolverine automatically matches handlers by return type
public class WolverineTestRequestHandler
{
    public WolverineTestResponse Handle(WolverineTestRequest request)
    {
        return new WolverineTestResponse { Result = $"Processed: {request.Data}" };
    }
}

// Wolverine-compatible notification model
// Wolverine uses IMessage for notifications (no response)
public class WolverineTestNotification : IMessage
{
    public string Message { get; set; } = string.Empty;
}

// Wolverine-compatible notification handlers
public class WolverineTestNotificationHandler1
{
    public void Handle(WolverineTestNotification notification)
    {
        // Simulate some work (identical to Coordix and MediatR handlers)
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler2
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler3
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler4
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler5
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler6
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler7
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler8
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler9
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

public class WolverineTestNotificationHandler10
{
    public void Handle(WolverineTestNotification notification)
    {
        var result = notification.Message.Length;
    }
}

