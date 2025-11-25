using AdvancedSample.Domain.Responses;
using Coordix.Interfaces;

namespace AdvancedSample.Domain.Queries;

public sealed record WeatherForecastQuery(
    Guid GuidId
) : IRequest<WeatherForecastResponse>;