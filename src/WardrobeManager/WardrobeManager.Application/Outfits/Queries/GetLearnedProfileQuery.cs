using MediatR;

namespace WardrobeManager.Application.Outfits.Queries;

public record GetLearnedProfileQuery(Guid UserId) : IRequest<LearnedProfileDto>;

// what the recommender has learned about a user — surfaced as read-only "insights".
public record LearnedProfileDto(
    IReadOnlyList<LearnedTasteDto> TopColors,
    IReadOnlyList<LearnedTasteDto> TopStyles,
    IReadOnlyList<LearnedTasteDto> AvoidedColors,
    IReadOnlyList<LearnedPairDto> StrongPairs,
    DateTime? UpdatedAt);

public record LearnedTasteDto(string Label, double Score);

public record LearnedPairDto(string ItemA, string ItemB, double Compatibility);
