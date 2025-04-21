using Coordix.Interfaces;

namespace AdvancedSample.Domain.Events;

public sealed class WeatherForecastEmailEvent : INotification
{
    public Guid EventId { get; set; }

    public WeatherForecastEmailEvent(Guid eventId) => EventId = eventId;
}
