using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record CreatePlannerEventCommand(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string> PreferredStyles, int? ReuseAfterDays = 3) : IRequest<Guid>;

public sealed class CreatePlannerEventCommandHandler : IRequestHandler<CreatePlannerEventCommand, Guid>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly TimeProvider _clock;

    public CreatePlannerEventCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        TimeProvider? clock = null)
    {
        _plannerEventRepository = plannerEventRepository;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<Guid> Handle(CreatePlannerEventCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = PlannerEvent.Create(
            request.UserId,
            request.Name,
            request.Type,
            request.Location,
            request.StartDate,
            request.EndDate,
            request.PreferredStyles,
            _clock.GetUtcNow().UtcDateTime,
            request.ReuseAfterDays);

        await _plannerEventRepository.AddAsync(plannerEvent, cancellationToken);

        return plannerEvent.Id;
    }
}
