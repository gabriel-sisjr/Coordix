using Coordix.Core.Extensions;
using Coordix.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.CoreTests.RobustnessTests;

public class ScopedHandlerTests
{
    // Scoped service to track instance lifecycle
    public class ScopedService
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
        public int CallCount { get; set; }
    }

    public class ScopedRequest : IRequest<Guid> { }

    public class ScopedRequestHandler : IRequestHandler<ScopedRequest, Guid>
    {
        private readonly ScopedService _scopedService;

        public ScopedRequestHandler(ScopedService scopedService)
        {
            _scopedService = scopedService;
        }

        public Task<Guid> Handle(ScopedRequest request, CancellationToken cancellationToken)
        {
            _scopedService.CallCount++;
            return Task.FromResult(_scopedService.InstanceId);
        }
    }

    [Fact]
    public async Task Send_WithScopedHandler_ShouldUseSameInstanceWithinScope()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddScoped<ScopedService>();
        services.AddScoped<IRequestHandler<ScopedRequest, Guid>, ScopedRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act - Within the same scope
        Guid instanceId1;
        Guid instanceId2;

        using (IServiceScope scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            instanceId1 = await mediator.Send(new ScopedRequest());
            instanceId2 = await mediator.Send(new ScopedRequest());
        }

        // Assert - Should be the same instance within scope
        Assert.Equal(instanceId1, instanceId2);
    }

    [Fact]
    public async Task Send_WithScopedHandler_ShouldUseDifferentInstancesAcrossScopes()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddScoped<ScopedService>();
        services.AddScoped<IRequestHandler<ScopedRequest, Guid>, ScopedRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act - Different scopes
        Guid instanceId1;
        Guid instanceId2;

        using (IServiceScope scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            instanceId1 = await mediator.Send(new ScopedRequest());
        }

        using (IServiceScope scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            instanceId2 = await mediator.Send(new ScopedRequest());
        }

        // Assert - Should be different instances across scopes
        Assert.NotEqual(instanceId1, instanceId2);
    }

    public class ScopedNotification : INotification { }

    public class ScopedNotificationHandler : INotificationHandler<ScopedNotification>
    {
        private readonly ScopedService _scopedService;

        public ScopedNotificationHandler(ScopedService scopedService)
        {
            _scopedService = scopedService;
        }

        public Task Handle(ScopedNotification notification, CancellationToken cancellationToken)
        {
            _scopedService.CallCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Publish_WithScopedHandlers_ShouldUseSameInstanceForAllHandlers()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddScoped<ScopedService>();
        services.AddScoped<INotificationHandler<ScopedNotification>, ScopedNotificationHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using IServiceScope scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        ScopedService scopedService = scope.ServiceProvider.GetRequiredService<ScopedService>();

        await mediator.Publish(new ScopedNotification());

        // Assert - Handler should have been called and modified the scoped service
        Assert.Equal(1, scopedService.CallCount);
    }
}

