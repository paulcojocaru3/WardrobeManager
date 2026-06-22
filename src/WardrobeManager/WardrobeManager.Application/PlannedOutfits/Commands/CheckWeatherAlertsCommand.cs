using MediatR;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public sealed record CheckWeatherAlertsCommand : IRequest<int>;
