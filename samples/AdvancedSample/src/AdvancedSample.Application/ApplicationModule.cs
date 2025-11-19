using AdvancedSample.Application.WeatherForecast;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedSample.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplicationModule(this IServiceCollection services)
    {
        // Example of injections of your own services.
        // Usually we have interfaces to invert the control.
        services.AddScoped<WeatherForecastHandler>();

        return services;
    }
}
