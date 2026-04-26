import { CLOTHING_TYPES } from '../constants/wardrobe';

export function toTypeIndex(typeValue) {
  if (typeof typeValue === 'number') {
    return typeValue;
  }

  if (typeof typeValue === 'string') {
    const index = CLOTHING_TYPES.indexOf(typeValue.toUpperCase());
    return index >= 0 ? index : -1;
  }

  return -1;
}

export function toCsv(value) {
  if (Array.isArray(value)) {
    return value.join(', ');
  }

  return value;
}

export function toStringArray(csvValue) {
  if (!csvValue || typeof csvValue !== 'string') {
    return [];
  }

  return csvValue.split(',').map((item) => item.trim()).filter(Boolean);
}

export function normalizeStatsResponse(data) {
  const SEASON_ORDER = ['spring', 'summer', 'fall', 'winter'];

  const v = (obj, key) => {
    if (!obj) return null;
    const target = key.toLowerCase();
    const actualKey = Object.keys(obj).find((k) => k.toLowerCase() === target);
    return actualKey ? obj[actualKey] : null;
  };

  const parseMonthLabel = (value) => {
    if (!value) return 0;
    const parsed = new Date(`01 ${value}`);
    return Number.isNaN(parsed.getTime()) ? 0 : parsed.getTime();
  };

  const getSeasonRank = (value) => {
    if (!value) return Number.MAX_SAFE_INTEGER;
    const index = SEASON_ORDER.indexOf(String(value).toLowerCase());
    return index === -1 ? Number.MAX_SAFE_INTEGER : index;
  };

  const monthlyActivity = Object.entries(v(data, 'monthlyActivity') || {})
    .map(([month, stat]) => ({
      month,
      total: v(stat, 'totalWears') ?? 0,
      unique: v(stat, 'uniqueItemsWorn') ?? 0
    }))
    .sort((a, b) => parseMonthLabel(a.month) - parseMonthLabel(b.month));

  const seasonalDist = Object.entries(v(data, 'seasonalDistribution') || {})
    .map(([season, stat]) => ({
      season,
      total: v(stat, 'totalWears') ?? 0,
      unique: v(stat, 'uniqueItemsWorn') ?? 0
    }))
    .sort((a, b) => getSeasonRank(a.season) - getSeasonRank(b.season));

  return {
    window: {
      label: v(v(data, 'window'), 'label') || 'all time',
      startDateUtc: v(v(data, 'window'), 'startDateUtc'),
      endDateUtc: v(v(data, 'window'), 'endDateUtc')
    },
    totalWearSessions: v(data, 'totalWearSessions') ?? 0,
    totalWearEvents: v(data, 'totalWearEvents') ?? 0,
    totalDistinctWornItems: v(data, 'totalDistinctWornItems') ?? 0,
    activeDays: v(data, 'activeDays') ?? 0,
    topWornItems: (v(data, 'topWornItems') || []).map((i) => ({
      id: v(i, 'id'), name: v(i, 'name'), imageUrl: v(i, 'imageUrl'), count: v(i, 'count') ?? 0
    })),
    unwornRecently: (v(data, 'unwornRecently') || []).map((i) => ({
      id: v(i, 'id'), name: v(i, 'name'), imageUrl: v(i, 'imageUrl'), days: v(i, 'daysSinceLastWear') ?? 0
    })),
    wornColors: (v(data, 'wornColors') || []).map((c) => ({
      color: v(c, 'color'), pct: v(c, 'percentage') ?? 0
    })),
    styleDist: v(data, 'styleDistribution') || {},
    styleByDay: v(data, 'styleByDay') || {},
    monthlyActivity,
    seasonalDist,
    topOutfits: (v(data, 'topOutfits') || []).map((o) => ({
      id: v(o, 'id'),
      name: v(o, 'name'),
      count: v(o, 'count') ?? 0,
      images: (v(o, 'itemImages') || []).filter(Boolean)
    })),
    utilizationRate: v(data, 'wardrobeUtilizationRate') ?? 0,
    diversityInsight: v(data, 'diversityInsight') || '',
    colorInsight: v(data, 'colorInsight') || '',
    wearHistory: (v(data, 'wearHistory') || []).map((d) => ({
      date: v(d, 'date'),
      outfits: (v(d, 'outfits') || []).map((s) => ({
        outfitId: v(s, 'outfitId'),
        outfitName: v(s, 'outfitName'),
        exactTime: v(s, 'exactTime'),
        itemImages: (v(s, 'itemImages') || []).filter(Boolean)
      }))
    })),
    streak: {
      currentStreakDays: v(v(data, 'streak'), 'currentStreakDays') ?? 0,
      longestStreakDays: v(v(data, 'streak'), 'longestStreakDays') ?? 0,
      latestWearDateUtc: v(v(data, 'streak'), 'latestWearDateUtc')
    },
    outfitSourceSplit: {
      totalSessions: v(v(data, 'outfitSourceSplit'), 'totalSessions') ?? 0,
      aiGeneratedSessions: v(v(data, 'outfitSourceSplit'), 'aiGeneratedSessions') ?? 0,
      customSessions: v(v(data, 'outfitSourceSplit'), 'customSessions') ?? 0,
      aiGeneratedPercentage: v(v(data, 'outfitSourceSplit'), 'aiGeneratedPercentage') ?? 0,
      customPercentage: v(v(data, 'outfitSourceSplit'), 'customPercentage') ?? 0
    },
    categoryUtilization: (v(data, 'categoryUtilization') || []).map((item) => ({
      category: v(item, 'category'),
      totalItems: v(item, 'totalItems') ?? 0,
      wornItems: v(item, 'wornItems') ?? 0,
      wearCount: v(item, 'wearCount') ?? 0,
      utilizationRate: v(item, 'utilizationRate') ?? 0
    }))
  };
}

export function getCssColor(colorName) {
  const map = {
    'navy blue': '#000080',
    'sky blue': '#87CEEB',
    maroon: '#800000',
    mustard: '#FFDB58',
    burgundy: '#800020',
    'olive green': '#556B2F',
    'off-white': '#FAF9F6',
    cream: '#FFFDD0',
    charcoal: '#36454F',
    grey: '#808080'
  };

  return map[colorName?.toLowerCase()] || colorName || '#999';
}

export function formatDate(value) {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return date.toLocaleDateString();
}

export function formatTime(value) {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}
