using System;
using System.Linq;
using System.Reflection;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coordix.CodeGen.Extensions;

public static class ServiceCollectionExtensions
{
    private const string GeneratedExecutorTypeName = "Coordix.Implementation.GeneratedHandlerExecutor";

    public static IServiceCollection AddCoordixWithCodeGen(this IServiceCollection services)
    {
        return AddCoordixWithCodeGen(services, configureOptions: null);
    }

    public static IServiceCollection AddCoordixWithCodeGen(
        this IServiceCollection services,
        Action<CoordixOptions>? configureOptions)
    {
        return AddCoordixWithCodeGen(services, configureOptions, Array.Empty<object>());
    }

    public static IServiceCollection AddCoordixWithCodeGen(
        this IServiceCollection services,
        Action<CoordixOptions>? configureOptions,
        params object[] args)
    {
        Type? generatedExecutorType = FindGeneratedExecutorType();

        if (generatedExecutorType == null)
        {
            throw new InvalidOperationException(
                $"Generated HandlerExecutor type '{GeneratedExecutorTypeName}' was not found. " +
                "Make sure the Coordix.CodeGen source generator is properly installed and the project has been built.");
        }

        Coordix.Extensions.ServiceCollectionExtensions.AddCoordix(
            services,
            options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
                configureOptions?.Invoke(options);
            },
            args);

        services.RemoveAll<IHandlerExecutor>();
        services.AddSingleton(typeof(IHandlerExecutor), generatedExecutorType);

        services.RemoveAll<CoordixOptions>();
        CoordixOptions finalOptions = new CoordixOptions
        {
            HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred
        };
        configureOptions?.Invoke(finalOptions);
        services.AddSingleton(finalOptions);

        return services;
    }

    private static Type? FindGeneratedExecutorType()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly? assembly in assemblies)
        {
            try
            {
                Type type = assembly.GetType(GeneratedExecutorTypeName);
                if (type != null && typeof(IHandlerExecutor).IsAssignableFrom(type))
                {
                    return type;
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }
}

