namespace WardrobeManager.Application.Abstractions;

public record CandidateItem(Guid ItemId, string Slot, string? ProcessedImageUrl);

public record OutfitCandidate(int CandidateId, double CumScore, IReadOnlyList<CandidateItem> Items);
