using AdvancedSample.Domain.Models;

namespace AdvancedSample.Domain.Responses;

public sealed record WeatherForecastResponse(
    Guid GuidId,
    WeatherForecast[] Weathers
);