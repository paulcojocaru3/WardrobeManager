using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Infrastructure.Persistance;
using WardrobeManager.Application.Users.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
    options.UseNpgsql(connectionString));

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));

// Register Repositories
builder.Services.AddScoped<IUserRepository, WardrobeManager.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<IClothingRepository, WardrobeManager.Infrastructure.Repositories.ClothingRepository>();

// Register External Services (Typed HttpClient)
builder.Services.AddHttpClient<WardrobeManager.Application.Abstractions.IMlService, WardrobeManager.Infrastructure.ExternalServices.MlService>(client =>
{
    var mlUrl = builder.Configuration["ExternalServices:MlApiUrl"];
    client.BaseAddress = new Uri(mlUrl ?? "http://localhost:8000");
});

var app = builder.Build();

// Automatically ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowReact");

app.MapControllers();
app.Run();