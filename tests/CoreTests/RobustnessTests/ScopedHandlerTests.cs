using Coordix.Extensions;
using Coordix.Implementation;
using Coordix.Interfaces;
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
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
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
        // Note: IMediator is singleton, but handlers are resolved from the scope's service provider
        // We need to get the mediator from root, but handlers will be resolved from scope
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Guid instanceId1;
        Guid instanceId2;

        using (IServiceScope scope1 = provider.CreateScope())
        {
            // Create a new HandlerExecutor for this scope to resolve handlers from scope
            HandlerExecutor scopeExecutor = new HandlerExecutor(scope1.ServiceProvider);
            Mediator scopeMediator = new Mediator(scopeExecutor);
            instanceId1 = await scopeMediator.Send(new ScopedRequest());
        }

        using (IServiceScope scope2 = provider.CreateScope())
        {
            HandlerExecutor scopeExecutor = new HandlerExecutor(scope2.ServiceProvider);
            Mediator scopeMediator = new Mediator(scopeExecutor);
            instanceId2 = await scopeMediator.Send(new ScopedRequest());
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
        // Don't use AddCoordix() to avoid auto-registration, register manually
        services.AddSingleton<IMediator, Mediator>();
        services.AddSingleton<IHandlerExecutor>(sp => new HandlerExecutor(sp));
        services.AddScoped<ScopedService>();
        services.AddScoped<INotificationHandler<ScopedNotification>, ScopedNotificationHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act - Create scope and use scope's service provider for HandlerExecutor
        using IServiceScope scope = provider.CreateScope();
        HandlerExecutor scopeExecutor = new HandlerExecutor(scope.ServiceProvider);
        Mediator scopeMediator = new Mediator(scopeExecutor);
        ScopedService scopedService = scope.ServiceProvider.GetRequiredService<ScopedService>();

        await scopeMediator.Publish(new ScopedNotification());

        // Assert - Handler should have been called once and modified the scoped service
        Assert.Equal(1, scopedService.CallCount);
    }
}

