namespace WardrobeManager.Application.Clothing.Queries;

// turns the raw range/customStart/customEnd query inputs into a UTC stats window
public static class StatsWindowResolver
{
    private static readonly HashSet<string> SupportedRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "7d",
        "30d",
        "90d",
        "1y",
        "all-time",
        "custom"
    };

    public static StatsWindowResolution Resolve(string? range, DateTime? customStart, DateTime? customEnd, DateTime? nowUtc = null)
    {
        var normalizedRange = string.IsNullOrWhiteSpace(range)
            ? null
            : range.Trim().ToLowerInvariant();

        if (normalizedRange is not null && !SupportedRanges.Contains(normalizedRange))
        {
            return StatsWindowResolution.Invalid($"Invalid range '{range}'. Allowed values: 7d, 30d, 90d, 1y, custom.");
        }

        var hasCustomStart = customStart.HasValue;
        var hasCustomEnd = customEnd.HasValue;
        var hasAnyCustomDate = hasCustomStart || hasCustomEnd;

        if (!hasAnyCustomDate && normalizedRange is null)
        {
            return StatsWindowResolution.Empty();
        }

        if (hasAnyCustomDate)
        {
            if (!hasCustomStart || !hasCustomEnd)
            {
                return StatsWindowResolution.Invalid("Both customStart and customEnd must be provided.");
            }

            if (normalizedRange is not null && normalizedRange != "custom")
            {
                return StatsWindowResolution.Invalid("When customStart/customEnd are provided, range must be omitted or set to 'custom'.");
            }

            var startUtc = NormalizeAsUtc(customStart!.Value).Date;
            var endUtc = NormalizeAsUtc(customEnd!.Value).Date.AddDays(1).AddTicks(-1);

            if (endUtc < startUtc)
            {
                return StatsWindowResolution.Invalid("customEnd must be greater than or equal to customStart.");
            }

            return StatsWindowResolution.Valid(startUtc, endUtc);
        }

        if (normalizedRange == "custom")
        {
            return StatsWindowResolution.Invalid("Range 'custom' requires both customStart and customEnd query parameters.");
        }

        var endDate = nowUtc ?? DateTime.UtcNow;
        var startDate = normalizedRange switch
        {
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            "1y" => endDate.AddYears(-1),
            _ => (DateTime?)null
        };

        if (!startDate.HasValue)
        {
            return StatsWindowResolution.Empty();
        }

        return StatsWindowResolution.Valid(startDate.Value, endDate);
    }

    private static DateTime NormalizeAsUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

public sealed record StatsWindowResolution(bool IsValid, DateTime? StartUtc, DateTime? EndUtc, string? Error)
{
    public static StatsWindowResolution Empty() => new(true, null, null, null);

    public static StatsWindowResolution Valid(DateTime startUtc, DateTime endUtc) => new(true, startUtc, endUtc, null);

    public static StatsWindowResolution Invalid(string error) => new(false, null, null, error);
}
