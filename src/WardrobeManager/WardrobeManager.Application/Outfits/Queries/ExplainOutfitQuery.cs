using MediatR;

namespace WardrobeManager.Application.Outfits.Queries;

// produces natural-language styling notes for an already-generated outfit. Stateless / read-only: it
public sealed record ExplainOutfitQuery(
    Guid UserId,
    IReadOnlyList<Guid> ItemIds,
    string? Style = null,
    string? Occasion = null,
    string? City = null,
    IReadOnlyList<string>? Tradeoffs = null) : IRequest<StylingNotesResult>;

public sealed record StylingNotesResult(IReadOnlyList<string> Notes);
