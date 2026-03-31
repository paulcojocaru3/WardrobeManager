using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Infrastructure.Persistance;
using WardrobeManager.Application.Users.Commands;
using FluentValidation;
using MediatR;
using WardrobeManager.Application.Users.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// Register MediatR and FluentValidation
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly);
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();

// Register Repositories
builder.Services.AddScoped<IUserRepository, WardrobeManager.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<IClothingRepository, WardrobeManager.Infrastructure.Repositories.ClothingRepository>();
builder.Services.AddScoped<IOutfitRepository, WardrobeManager.Infrastructure.Repositories.OutfitRepository>();

// Register Domain/Application Services
builder.Services.AddScoped<IOutfitGenerator, WardrobeManager.Application.Outfits.OutfitGenerator>();

// Register extern
builder.Services.AddHttpClient<IMlService, WardrobeManager.Infrastructure.ExternalServices.MlService>(client =>
{
    var mlUrl = builder.Configuration["FastApi:BaseUrl"] ?? builder.Configuration["ExternalServices:MlApiUrl"];
    client.BaseAddress = new Uri(mlUrl ?? "http://localhost:8000");
});

var app = builder.Build();

// Global Exception Handler for all Errors
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new
        {
            Errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
        });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = ex.Message,
            Type = ex.GetType().Name
        });
    }
});

// database creation with retry logic
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    int retries = 5;
    while (retries > 0)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception)
        {
            retries--;
            if (retries == 0) throw;
            Thread.Sleep(5000); 
        }
    }
}

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.UseCors("AllowReact");

app.MapControllers();
app.Run();