namespace WardrobeManager.Application.Clothing.Queries;

public class WearStatisticsDto
{
    // 0. Window & Summary
    public StatsWindowDto Window { get; set; } = new();
    public int TotalWearSessions { get; set; }
    public int TotalWearEvents { get; set; }
    public int TotalDistinctWornItems { get; set; }
    public int ActiveDays { get; set; }

    // 1. Usage
    public List<ItemUsageDto> TopWornItems { get; set; } = new();
    public List<ItemUsageDto> LeastWornItems { get; set; } = new();
    public List<ItemUsageDto> UnwornRecently { get; set; } = new();
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();

    // 2. Colors
    public List<ColorStatDto> WardrobeColors { get; set; } = new();
    public List<ColorStatDto> WornColors { get; set; } = new();
    public string ColorInsight { get; set; } = string.Empty;

    // 3. Style
    public Dictionary<string, double> StyleDistribution { get; set; } = new();
    public Dictionary<string, string> StyleByDay { get; set; } = new();

    // 4. Outfits
    public List<OutfitUsageDto> TopOutfits { get; set; } = new();
    public List<ItemUsageDto> MostFrequentInOutfits { get; set; } = new();

    // 5. Temporal
    public Dictionary<string, TemporalStatDto> SeasonalDistribution { get; set; } = new();
    public Dictionary<string, TemporalStatDto> MonthlyActivity { get; set; } = new();

    // 7. History (Grouped by Day)
    public List<DailyHistoryDto> WearHistory { get; set; } = new();

    // 6. Diversity
    public double WardrobeUtilizationRate { get; set; }
    public string DiversityInsight { get; set; } = string.Empty;

    // 8. Option 2 analytics
    public StreakStatsDto Streak { get; set; } = new();
    public OutfitSourceSplitDto OutfitSourceSplit { get; set; } = new();
    public List<CategoryUtilizationDto> CategoryUtilization { get; set; } = new();
}

public class StatsWindowDto
{
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class StreakStatsDto
{
    public int CurrentStreakDays { get; set; }
    public int LongestStreakDays { get; set; }
    public DateTime? LatestWearDateUtc { get; set; }
}

public class OutfitSourceSplitDto
{
    public int TotalSessions { get; set; }
    public int AiGeneratedSessions { get; set; }
    public int CustomSessions { get; set; }
    public double AiGeneratedPercentage { get; set; }
    public double CustomPercentage { get; set; }
}

public class CategoryUtilizationDto
{
    public string Category { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int WornItems { get; set; }
    public int WearCount { get; set; }
    public double UtilizationRate { get; set; }
}

public class DailyHistoryDto
{
    public DateTime Date { get; set; }
    public List<WornOutfitDto> Outfits { get; set; } = new();
}

public class WornOutfitDto
{
    public Guid? OutfitId { get; set; }
    public string OutfitName { get; set; } = "Custom Look";
    public DateTime ExactTime { get; set; }
    public List<string> ItemImages { get; set; } = new();
}

public class TemporalStatDto
{
    public int TotalWears { get; set; }
    public int UniqueItemsWorn { get; set; }
}

public class ItemUsageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Count { get; set; }
    public int? DaysSinceLastWear { get; set; }
}

public class ColorStatDto
{
    public string Color { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class OutfitUsageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> ItemImages { get; set; } = new();
}
