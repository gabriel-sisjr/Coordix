using AdvancedSample.Domain.Queries;
using Coordix.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedSample.API.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class WeatherForecastController(
    ILogger<WeatherForecastController> logger,
    IMediator mediator) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        logger.LogInformation("Getting Weather for ID: {@id}", id);

        var query = new WeatherForecastQuery(id);
        var response = await mediator.Send(query, cancellationToken);

        return Ok(response);
    }
}
