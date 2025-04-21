using AdvancedSample.Domain.Events;
using AdvancedSample.Domain.Queries;
using AdvancedSample.Domain.Responses;
using Coordix.Interfaces;

namespace AdvancedSample.Application.WeatherForecast;

public sealed class WeatherForecastHandler(IMediator mediator) : IRequestHandler<WeatherForecastQuery, WeatherForecastResponse>
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public async Task<WeatherForecastResponse> Handle(WeatherForecastQuery request, CancellationToken cancellationToken)
    {
        var weathers = Enumerable.Range(1, 5).Select(index =>
            {
                var date = DateOnly.FromDateTime(DateTime.Now.AddDays(index));
                var temperatureC = Random.Shared.Next(-20, 55);
                var temperatureF = temperatureC * 9 / 5 + 32;
                var summary = Summaries[Random.Shared.Next(Summaries.Length)];
                return new Domain.Models.WeatherForecast(date, temperatureC, temperatureF, summary);
            }).ToArray();

        var response = new WeatherForecastResponse(request.GuidId, weathers);

        // Creating and Publish an Event
        var notification = new WeatherForecastEmailEvent(request.GuidId);
        await mediator.Publish(notification, cancellationToken);

        return await Task.FromResult(response);
    }
}
