using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Infrastructure.ExternalServices;
using WardrobeManager.Infrastructure.Persistance;
using WardrobeManager.Infrastructure.Repositories;
using WardrobeManager.Infrastructure.Security;

namespace WardrobeManager.Infrastructure;

// persistence, repositories, security, external services
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        // persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, o => o.UseVector()));

        // repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClothingRepository, ClothingRepository>();
        services.AddScoped<IOutfitRepository, OutfitRepository>();
        services.AddScoped<IWearEventRepository, WearEventRepository>();
        services.AddScoped<IPlannerEventRepository, PlannerEventRepository>();
        services.AddScoped<IOutfitFeedbackRepository, OutfitFeedbackRepository>();
        services.AddScoped<IUserEvaluatorWeightsRepository, UserEvaluatorWeightsRepository>();

        // security
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // weather — typed client with a tight timeout; it's a small JSON API
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
            client.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();

        // deterministic keyword maps (JSON under the host content root /Data)
        services.AddSingleton<IOccasionClassifier>(_ =>
            new OccasionClassifier(Path.Combine(contentRootPath, "Data", "occasion-style-map.json")));
        services.AddSingleton<IGarmentClassifier>(_ =>
            new GarmentClassifier(Path.Combine(contentRootPath, "Data", "garment-keyword-map.json")));

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
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120); // must be >= 2x attempt timeout
        });

        services.AddHttpClient<IPromptIntentService, OllamaPromptIntentService>(client =>
        {
            var ollamaUrl = configuration["Ollama:BaseUrl"];
            if (ollamaUrl == null)
            {
                ollamaUrl = "http://localhost:11434";
            }
            client.BaseAddress = new Uri(ollamaUrl.TrimEnd('/') + "/");

            int timeout;
            if (int.TryParse(configuration["Ollama:TimeoutSeconds"], out var s))
            {
                timeout = s;
            }
            else
            {
                timeout = 60;
            }
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        return services;
    }
}
