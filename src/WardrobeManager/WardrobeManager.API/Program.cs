using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using WardrobeManager.API.Middleware;
using WardrobeManager.Application;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Infrastructure;

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
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The production container is reachable only through the Docker reverse proxy.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddScoped<WardrobeManager.Application.Abstractions.INotificationPushGateway,
    WardrobeManager.API.Notifications.SignalRNotificationDispatcher>();

// cors for the React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(origin => origin.Value)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Cast<string>()
            .ToArray();
        if (origins.Length == 0)
        {
            origins = ["http://localhost:5173"];
        }

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // signalr's negotiate runs with credentials; without this the browser blocks the live
              .AllowCredentials();
    });
});

// compose the layers — registrations live next to each layer (see *.DependencyInjection)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

// background worker that re-checks planned-event weather and raises drift notifications.
builder.Services.AddHostedService<WardrobeManager.API.BackgroundServices.WeatherWatchBackgroundService>();
// auth pipeline (host concern)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured.");
}
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // websockets can't send the Authorization header, so the SignalR client passes the JWT as a
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                else if (context.Request.Cookies.TryGetValue("wardrobe_auth", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionMiddleware>();

// database creation with retry logic (the DB container may still be starting)
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IApplicationDbInitializer>();
    await dbInitializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WardrobeManager.API.Hubs.NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");
await app.RunAsync();
