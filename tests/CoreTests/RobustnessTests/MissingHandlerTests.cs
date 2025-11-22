using Coordix.Core.Extensions;
using Coordix.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.CoreTests.RobustnessTests;

public class MissingHandlerTests
{
    public class UnregisteredRequest : IRequest<string> { }

    public class UnregisteredNotification : INotification { }

    [Fact]
    public async Task Send_WhenNoHandlerRegistered_ShouldThrowClearException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        ServiceProvider provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new UnregisteredRequest())
        );

        // Should provide a clear error message
        Assert.Contains("handler", exception.Message.ToLower());
        Assert.Contains("UnregisteredRequest", exception.Message);
    }

    [Fact]
    public async Task Publish_WhenNoHandlerRegistered_ShouldCompleteSuccessfully()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        ServiceProvider provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Should not throw, just complete
        await mediator.Publish(new UnregisteredNotification());

        // Assert - No exception means success
        Assert.True(true);
    }

    [Fact]
    public void GetRequiredService_WhenMediatorNotRegistered_ShouldThrowClearException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        // Intentionally not calling AddCoordix()
        ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IMediator>()
        );

        Assert.Contains("IMediator", exception.Message);
    }
}

