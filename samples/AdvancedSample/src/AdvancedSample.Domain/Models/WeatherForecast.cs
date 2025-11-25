namespace AdvancedSample.Domain.Models;

public sealed record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    int TemperatureF,
    string Summary
);
