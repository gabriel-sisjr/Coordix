using AdvancedSample.Domain.Events;
using Coordix.Interfaces;

namespace AdvancedSample.Service.Email;

public sealed class WeatherForecastEmailHandler : INotificationHandler<WeatherForecastEmailEvent>
{
    public Task Handle(WeatherForecastEmailEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending email requested by Notification: {notification.EventId}");
        return Task.CompletedTask;
    }
}
