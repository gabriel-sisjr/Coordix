using Microsoft.Extensions.DependencyInjection;

namespace AdvancedSample.Service.Email;

public static class ServiceEmailModule
{
    public static IServiceCollection AddServiceEmailModule(this IServiceCollection services)
    {
        // Example of injections of your own services.
        // Usually we have interfaces to invert the control.
        services.AddScoped<WeatherForecastEmailHandler>();

        return services;
    }
}
