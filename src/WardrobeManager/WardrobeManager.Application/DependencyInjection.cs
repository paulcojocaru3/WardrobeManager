using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Application.PlannedOutfits;

namespace WardrobeManager.Application;

// MediatR handlers, validators, pure domain services
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Outfit scoring evaluators — stateless and thread-safe, so registered as singletons.
        services.AddSingleton<IOutfitEvaluator, WeatherEvaluator>();
        services.AddSingleton<IOutfitEvaluator, StyleEvaluator>();
        services.AddSingleton<IOutfitEvaluator, ColorHarmonyEvaluator>();
        services.AddSingleton<IOutfitEvaluator, ColorPreferenceEvaluator>();
        services.AddSingleton<IOutfitEvaluator, VarietyEvaluator>();

        services.AddScoped<IOutfitGenerator, OutfitGenerator>();
        services.AddScoped<IStartItemSelector, StartItemSelector>();
        services.AddScoped<IWeightLearningService, WeightLearningService>();
        services.AddScoped<IEventOutfitPlanningService, EventOutfitPlanningService>();

        return services;
    }
}
