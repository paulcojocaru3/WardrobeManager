using MediatR;
using WardrobeManager.Application.PlannedOutfits.Commands;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

// synthetic weather alert to preview the alert banner without waiting for real forecast drift
public record GetTestAlertQuery(Guid UserId) : IRequest<WeatherAlertDto?>;

public sealed class GetTestAlertQueryHandler : IRequestHandler<GetTestAlertQuery, WeatherAlertDto?>
{
    public Task<WeatherAlertDto?> Handle(GetTestAlertQuery request, CancellationToken cancellationToken)
    {
        var eventDate = DateTime.UtcNow.Date.AddDays(2);

        var alert = new WeatherAlertDto(
            IsAvailable: true,
            IsSignificantChange: true,
            TemperatureDelta: -7f,
            StoredForecast: new WeatherDataDto(22f, "Sunny", "Summer", eventDate),
            CurrentWeather: new WeatherDataDto(15f, "Rain", "Fall", eventDate),
            EventName: "Test Event",
            EventDate: eventDate,
            PlannerEventId: Guid.Empty
        );

        return Task.FromResult<WeatherAlertDto?>(alert);
    }
}
