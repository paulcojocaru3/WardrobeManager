using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Common.Behaviors;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Application.Notifications;
using WardrobeManager.Application.PlannedOutfits;

namespace WardrobeManager.Application;

// mediatr handlers, validators, pure domain services
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);

        // outfit scoring evaluators — stateless and thread-safe, so registered as singletons.
        services.AddSingleton<IOutfitEvaluator, WeatherEvaluator>();
        services.AddSingleton<IOutfitEvaluator, StyleEvaluator>();
        services.AddSingleton<IOutfitEvaluator, FormalityCoherenceEvaluator>();
        services.AddSingleton<IOutfitEvaluator, ColorHarmonyEvaluator>();
        services.AddSingleton<IOutfitEvaluator, ColorPreferenceEvaluator>();
        services.AddSingleton<IOutfitEvaluator, WearRotationEvaluator>();
        services.AddSingleton<IOutfitEvaluator, PairAffinityEvaluator>();
        services.AddSingleton<IOutfitEvaluator, TasteProfileEvaluator>();

        // stage 1 of generation: the single hard-constraint authority (relaxable feasibility),
        services.AddSingleton<IGarmentFeasibility, GarmentFeasibility>();

        services.AddScoped<IOutfitGenerator, BeamSearchOutfitGenerator>();
        services.AddScoped<StylistCandidatePoolBuilder>();
        services.AddScoped<StylistOutfitComposer>();
        services.AddScoped<IStartItemSelector, StartItemSelector>();
        services.AddScoped<IEventOutfitPlanningService, EventOutfitPlanningService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // behaviour learning (item-pair compatibility + color/style taste), driven by feedback.
        services.AddScoped<ItemPairLearningService>();
        services.AddScoped<UserLearningProfileService>();
        services.AddScoped<IFeedbackLearningCoordinator, FeedbackLearningCoordinator>();

        return services;
    }
}
