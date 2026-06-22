using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Infrastructure.ExternalServices;
using WardrobeManager.Infrastructure.Persistance;
using WardrobeManager.Infrastructure.Repositories;
using WardrobeManager.Infrastructure.Security;

namespace WardrobeManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, o => o.UseVector()));
        services.AddScoped<IApplicationDbInitializer, ApplicationDbInitializer>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClothingRepository, ClothingRepository>();
        services.AddScoped<IOutfitRepository, OutfitRepository>();
        services.AddScoped<IWearEventRepository, WearEventRepository>();
        services.AddScoped<IPlannerEventRepository, PlannerEventRepository>();
        services.AddScoped<IOutfitFeedbackRepository, OutfitFeedbackRepository>();
        services.AddScoped<IItemPairScoreRepository, ItemPairScoreRepository>();
        services.AddScoped<IUserLearningProfileRepository, UserLearningProfileRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHttpClient<IWeatherService, WeatherService>(client =>
            client.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();

        services.AddSingleton<IThermalRules>(_ =>
            new ThermalRules(Path.Combine(contentRootPath, "Data", "thermal-rules.json")));

        services.AddSingleton<IOccasionFormalityRules>(_ =>
            new OccasionFormalityRules(Path.Combine(contentRootPath, "Data", "occasion-formality.json")));

        services.AddHttpClient<IMlService, MlService>(client =>
        {
            var mlUrl = configuration["FastApi:BaseUrl"];
            if (mlUrl == null)
            {
                mlUrl = configuration["ExternalServices:MlApiUrl"];
            }
            if (mlUrl == null)
            {
                mlUrl = "http://localhost:8000";
            }
            client.BaseAddress = new Uri(mlUrl);
            client.Timeout = TimeSpan.FromSeconds(120);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
            options.Retry.MaxRetryAttempts = 2;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
        });

        services.AddSingleton<IStylingNotesService, TemplateStylingNotesService>();

        // compose outfits with gemma3 over fashionclip candidates.
        services.AddSingleton(new StylistSettings
        {
            Enabled = bool.TryParse(configuration["Outfits:Stylist:Enabled"], out var se) && se,
            MaxCandidates = int.TryParse(configuration["Outfits:Stylist:MaxCandidates"], out var mc) ? mc : 24,
            MmrLambda = double.TryParse(configuration["Outfits:Stylist:MmrLambda"], out var ml) ? ml : 0.7
        });
        services.AddHttpClient<IOutfitStylist, OllamaOutfitStylist>(client =>
        {
            var ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(ollamaUrl.TrimEnd('/') + "/");

            var timeout = int.TryParse(configuration["Ollama:VisionTimeoutSeconds"], out var st) ? st : 120;
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        return services;
    }
}
