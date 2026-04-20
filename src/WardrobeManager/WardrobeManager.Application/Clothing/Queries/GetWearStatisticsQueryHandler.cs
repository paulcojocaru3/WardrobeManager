using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Clothing.Queries;

public record GetWearStatisticsQuery(Guid UserId, DateTime? StartDateUtc = null, DateTime? EndDateUtc = null) : IRequest<WearStatisticsDto>;

public class GetWearStatisticsQueryHandler : IRequestHandler<GetWearStatisticsQuery, WearStatisticsDto>
{
    private readonly IWearEventRepository _wearRepository;
    private readonly IClothingRepository _clothingRepository;
    private readonly IOutfitRepository _outfitRepository;

    public GetWearStatisticsQueryHandler(
        IWearEventRepository wearRepository, 
        IClothingRepository clothingRepository,
        IOutfitRepository outfitRepository)
    {
        _wearRepository = wearRepository;
        _clothingRepository = clothingRepository;
        _outfitRepository = outfitRepository;
    }

    public async Task<WearStatisticsDto> Handle(GetWearStatisticsQuery request, CancellationToken ct)
    {
        var allEvents = (await _wearRepository.GetAllByUserIdAsync(request.UserId)).ToList();
        var allClothes = (await _clothingRepository.GetByUserIdAsync(request.UserId, ct)).ToList();
        var allOutfits = await _outfitRepository.GetByUserIdAsync(request.UserId, ct);

        var dto = new WearStatisticsDto();
        var filteredEvents = ApplyTimeWindowFilter(allEvents, request.StartDateUtc, request.EndDateUtc).ToList();
        var sessionGroups = GroupBySession(filteredEvents).ToList();

        dto.Window = BuildStatsWindow(request.StartDateUtc, request.EndDateUtc);
        dto.TotalWearEvents = filteredEvents.Count;
        dto.TotalWearSessions = sessionGroups.Count;
        dto.TotalDistinctWornItems = filteredEvents.Select(e => e.ClothingItemId).Distinct().Count();
        dto.ActiveDays = filteredEvents.Select(e => e.WearDate.Date).Distinct().Count();

        dto.Streak = BuildStreakStats(sessionGroups.Select(g => g.SessionDate));
        dto.OutfitSourceSplit = BuildOutfitSourceSplit(sessionGroups, allOutfits);
        dto.CategoryUtilization = BuildCategoryUtilization(filteredEvents, allClothes);

        if (!allClothes.Any()) return dto;

        // 1. Usage Statistics
        var itemCounts = filteredEvents
            .Where(e => e.ClothingItem != null)
            .GroupBy(e => e.ClothingItemId)
            .Select(g => new { Id = g.Key, Count = g.Count(), LastWear = g.Max(e => e.WearDate) })
            .ToList();

        dto.TopWornItems = itemCounts
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select(x => MapToItemUsage(allClothes.FirstOrDefault(c => c.Id == x.Id), x.Count, x.LastWear))
            .ToList();

        dto.UnwornRecently = itemCounts
            .Where(x => (DateTime.UtcNow - x.LastWear).TotalDays > 30)
            .OrderByDescending(x => (DateTime.UtcNow - x.LastWear).TotalDays)
            .Take(5)
            .Select(x => MapToItemUsage(allClothes.FirstOrDefault(c => c.Id == x.Id), x.Count, x.LastWear))
            .ToList();

        // 2. Color Statistics
        dto.WardrobeColors = allClothes
            .GroupBy(c => c.Color?.ToLower() ?? "unknown")
            .Select(g => new ColorStatDto { Color = g.Key, Count = g.Count(), Percentage = (double)g.Count() / allClothes.Count * 100 })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (filteredEvents.Any())
        {
            dto.WornColors = filteredEvents
                .Where(e => e.ClothingItem != null)
                .GroupBy(e => e.ClothingItem!.Color?.ToLower() ?? "unknown")
                .Select(g => new ColorStatDto { Color = g.Key, Count = g.Count(), Percentage = (double)g.Count() / filteredEvents.Count * 100 })
                .OrderByDescending(x => x.Count)
                .ToList();

            var topColor = dto.WornColors.FirstOrDefault();
            if (topColor != null)
                dto.ColorInsight = $"{topColor.Percentage:F0}% of your looks feature {topColor.Color}.";
        }

        // 3. Style Statistics
        var styles = filteredEvents
            .Where(e => e.ClothingItem != null && !string.IsNullOrEmpty(e.ClothingItem.Usage))
            .SelectMany(e => e.ClothingItem!.Usage!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => s.Trim().ToLower())
            .ToList();

        if (styles.Any())
        {
            dto.StyleDistribution = styles
                .GroupBy(s => s)
                .ToDictionary(g => g.Key, g => (double)g.Count() / styles.Count * 100);
        }

        dto.StyleByDay = filteredEvents
            .Where(e => e.ClothingItem != null && !string.IsNullOrEmpty(e.ClothingItem.Usage))
            .GroupBy(e => e.WearDate.DayOfWeek)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.SelectMany(e => e.ClothingItem!.Usage!.Split(',')).GroupBy(s => s.Trim()).OrderByDescending(gs => gs.Count()).First().Key
            );

        // 4. Outfits - Normalized with Images
        dto.TopOutfits = filteredEvents
            .Where(e => e.OutfitId.HasValue)
            .GroupBy(e => e.OutfitId!.Value)
            .Select(g => {
                var outfit = allOutfits.FirstOrDefault(o => o.Id == g.Key);
                return new OutfitUsageDto { 
                    Id = g.Key, 
                    Name = outfit?.Name ?? "Unnamed Outfit",
                    Count = g.Select(e => e.WearDate.Date).Distinct().Count(),
                    ItemImages = outfit?.Items.Select(i => i.ProcessedImageUrl).ToList() ?? new List<string>()
                };
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        // 5. Temporal - STRICTLY OUTFIT WEARS & UNIQUE ITEMS
        dto.MonthlyActivity = filteredEvents
            .GroupBy(e => e.WearDate.ToString("MMM yyyy"))
            .ToDictionary(
                g => g.Key, 
                g => new TemporalStatDto { 
                    TotalWears = g.Select(e => new { e.OutfitId, e.WearDate.Date }).Distinct().Count(), // Count distinct outfit-day combinations
                    UniqueItemsWorn = g.Select(e => e.ClothingItemId).Distinct().Count() 
                }
            );

        dto.SeasonalDistribution = filteredEvents
            .GroupBy(e => GetSeason(e.WearDate))
            .ToDictionary(
                g => g.Key, 
                g => new TemporalStatDto { 
                    TotalWears = g.Select(e => new { e.OutfitId, e.WearDate.Date }).Distinct().Count(), 
                    UniqueItemsWorn = g.Select(e => e.ClothingItemId).Distinct().Count() 
                }
            );

        // 7. Wear History (Last 30 days) - Grouped by Day, then by Session
        dto.WearHistory = filteredEvents
            .GroupBy(e => e.WearDate.Date)
            .OrderByDescending(g => g.Key)
            .Select(dayGroup => new DailyHistoryDto {
                Date = dayGroup.Key,
                Outfits = dayGroup
                    .GroupBy(e => e.WearDate) // Group by exact timestamp to separate distinct "Wear Today" clicks
                    .Select(sessionGroup => new WornOutfitDto {
                        OutfitId = sessionGroup.First().OutfitId,
                        OutfitName = allOutfits.FirstOrDefault(o => o.Id == sessionGroup.First().OutfitId)?.Name ?? "Custom Look",
                        ExactTime = sessionGroup.Key,
                        ItemImages = sessionGroup.Where(e => e.ClothingItem != null).Select(e => e.ClothingItem!.ProcessedImageUrl).ToList()
                    })
                    .OrderByDescending(s => s.ExactTime)
                    .ToList()
            })
            .ToList();

        // 6. Diversity
        var uniqueItemsWornTotal = filteredEvents.Select(e => e.ClothingItemId).Distinct().Count();
        dto.WardrobeUtilizationRate = (double)uniqueItemsWornTotal / allClothes.Count * 100;
        dto.DiversityInsight = $"You've used {dto.WardrobeUtilizationRate:F0}% of your wardrobe.";

        return dto;
    }

    private ItemUsageDto MapToItemUsage(ClothingItem? item, int count, DateTime? lastWear)
    {
        if (item == null) return new ItemUsageDto();
        return new ItemUsageDto {
            Id = item.Id,
            Name = item.Name,
            ImageUrl = item.ProcessedImageUrl,
            Count = count,
            DaysSinceLastWear = lastWear.HasValue ? (int)(DateTime.UtcNow - lastWear.Value).TotalDays : null
        };
    }

    private string GetSeason(DateTime date)
    {
        int month = date.Month;
        if (month >= 3 && month <= 5) return "Spring";
        if (month >= 6 && month <= 8) return "Summer";
        if (month >= 9 && month <= 11) return "Fall";
        return "Winter";
    }

    private static IEnumerable<WearEvent> ApplyTimeWindowFilter(IEnumerable<WearEvent> events, DateTime? startDateUtc, DateTime? endDateUtc)
    {
        var query = events.AsEnumerable();

        if (startDateUtc.HasValue)
        {
            query = query.Where(e => e.WearDate >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(e => e.WearDate <= endDateUtc.Value);
        }

        return query;
    }

    private static StatsWindowDto BuildStatsWindow(DateTime? startDateUtc, DateTime? endDateUtc)
    {
        if (!startDateUtc.HasValue && !endDateUtc.HasValue)
        {
            return new StatsWindowDto
            {
                StartDateUtc = null,
                EndDateUtc = null,
                Label = "all time"
            };
        }

        var startLabel = startDateUtc?.ToString("yyyy-MM-dd") ?? "start";
        var endLabel = endDateUtc?.ToString("yyyy-MM-dd") ?? "today";

        return new StatsWindowDto
        {
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            Label = $"{startLabel} → {endLabel}"
        };
    }

    private static List<WearSessionGroup> GroupBySession(IEnumerable<WearEvent> events)
    {
        return events
            .GroupBy(e => new { e.OutfitId, e.WearDate })
            .Select(g => new WearSessionGroup
            {
                OutfitId = g.Key.OutfitId,
                SessionDate = g.Key.WearDate,
                Events = g.ToList()
            })
            .OrderBy(s => s.SessionDate)
            .ToList();
    }

    private static StreakStatsDto BuildStreakStats(IEnumerable<DateTime> sessionDates)
    {
        var sortedDistinctDates = sessionDates
            .Select(d => d.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!sortedDistinctDates.Any())
        {
            return new StreakStatsDto();
        }

        var longest = 1;
        var currentRun = 1;
        for (var i = 1; i < sortedDistinctDates.Count; i++)
        {
            var isConsecutive = sortedDistinctDates[i] == sortedDistinctDates[i - 1].AddDays(1);
            if (isConsecutive)
            {
                currentRun++;
            }
            else
            {
                currentRun = 1;
            }

            if (currentRun > longest)
            {
                longest = currentRun;
            }
        }

        var latestDate = sortedDistinctDates[^1];
        var runningCurrent = 1;
        for (var i = sortedDistinctDates.Count - 2; i >= 0; i--)
        {
            if (sortedDistinctDates[i] == sortedDistinctDates[i + 1].AddDays(-1))
            {
                runningCurrent++;
                continue;
            }

            break;
        }

        var today = DateTime.UtcNow.Date;
        var daysSinceLatest = (today - latestDate).TotalDays;
        var currentStreakDays = 0;
        if (daysSinceLatest <= 1)
        {
            currentStreakDays = runningCurrent;
        }

        return new StreakStatsDto
        {
            CurrentStreakDays = currentStreakDays,
            LongestStreakDays = longest,
            LatestWearDateUtc = latestDate
        };
    }

    private static OutfitSourceSplitDto BuildOutfitSourceSplit(IEnumerable<WearSessionGroup> sessions, IReadOnlyCollection<Outfit> outfits)
    {
        var outfitById = outfits.ToDictionary(o => o.Id, o => o);
        var totalSessions = 0;
        var aiSessions = 0;
        var customSessions = 0;

        foreach (var session in sessions)
        {
            totalSessions++;

            if (!session.OutfitId.HasValue)
            {
                customSessions++;
                continue;
            }

            if (!outfitById.TryGetValue(session.OutfitId.Value, out var outfit))
            {
                customSessions++;
                continue;
            }

            if (outfit.IsAiGenerated)
            {
                aiSessions++;
            }
            else
            {
                customSessions++;
            }
        }

        var aiPercentage = CalculatePercentage(aiSessions, totalSessions);
        var customPercentage = CalculatePercentage(customSessions, totalSessions);

        return new OutfitSourceSplitDto
        {
            TotalSessions = totalSessions,
            AiGeneratedSessions = aiSessions,
            CustomSessions = customSessions,
            AiGeneratedPercentage = aiPercentage,
            CustomPercentage = customPercentage
        };
    }

    private static List<CategoryUtilizationDto> BuildCategoryUtilization(IEnumerable<WearEvent> events, IReadOnlyCollection<ClothingItem> allClothes)
    {
        var wearsByItem = events
            .GroupBy(e => e.ClothingItemId)
            .ToDictionary(g => g.Key, g => g.Count());

        return allClothes
            .GroupBy(c => c.Type)
            .Select(g =>
            {
                var totalItems = g.Count();
                var wornItems = g.Count(item => wearsByItem.ContainsKey(item.Id));
                var wearCount = g.Sum(item => wearsByItem.GetValueOrDefault(item.Id));
                var utilizationRate = CalculatePercentage(wornItems, totalItems);

                return new CategoryUtilizationDto
                {
                    Category = g.Key.ToString(),
                    TotalItems = totalItems,
                    WornItems = wornItems,
                    WearCount = wearCount,
                    UtilizationRate = utilizationRate
                };
            })
            .OrderByDescending(c => c.WearCount)
            .ToList();
    }

    private static double CalculatePercentage(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return (double)value / total * 100;
    }

    private sealed class WearSessionGroup
    {
        public Guid? OutfitId { get; init; }
        public DateTime SessionDate { get; init; }
        public IReadOnlyCollection<WearEvent> Events { get; init; } = Array.Empty<WearEvent>();
    }
}
