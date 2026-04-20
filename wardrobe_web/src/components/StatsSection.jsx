import React, { useEffect, useMemo, useState } from 'react';
import axios from 'axios';
import Modal from '../components/Modal';
import './StatsSection.css';

const API_BASE_URL = 'http://localhost:5150/api';
const SEASON_ORDER = ['spring', 'summer', 'fall', 'winter'];

const RANGE_OPTIONS = [
  { value: '7d', label: '7d' },
  { value: '30d', label: '30d' },
  { value: '90d', label: '90d' },
  { value: '1y', label: '1y' },
  { value: 'custom', label: 'custom' }
];

const DEFAULT_STATS = {
  window: { label: 'all time', startDateUtc: null, endDateUtc: null },
  totalWearSessions: 0,
  totalWearEvents: 0,
  totalDistinctWornItems: 0,
  activeDays: 0,
  topWornItems: [],
  unwornRecently: [],
  wornColors: [],
  styleDist: {},
  styleByDay: {},
  monthlyActivity: [],
  seasonalDist: [],
  topOutfits: [],
  utilizationRate: 0,
  diversityInsight: '',
  colorInsight: '',
  wearHistory: [],
  streak: { currentStreakDays: 0, longestStreakDays: 0, latestWearDateUtc: null },
  outfitSourceSplit: {
    totalSessions: 0,
    aiGeneratedSessions: 0,
    customSessions: 0,
    aiGeneratedPercentage: 0,
    customPercentage: 0
  },
  categoryUtilization: []
};

const StatsSection = ({ userId }) => {
  const [stats, setStats] = useState(DEFAULT_STATS);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState('overview');
  const [previewOutfit, setPreviewOutfit] = useState(null);

  const [selectedRange, setSelectedRange] = useState('30d');
  const [customStart, setCustomStart] = useState('');
  const [customEnd, setCustomEnd] = useState('');
  const [rangeError, setRangeError] = useState('');
  const isCustomRange = selectedRange === 'custom';
  const isCustomRangeIncomplete = isCustomRange && (!customStart || !customEnd);
  const isCustomRangeInvalid = isCustomRange && customStart && customEnd && customStart > customEnd;
  const canApplyCustomRange = isCustomRange && !isCustomRangeIncomplete && !isCustomRangeInvalid;

  const fetchStats = async ({ manual = false } = {}) => {
    if (!userId) {
      return;
    }

    if (isCustomRange && !manual) {
      return;
    }

    if (isCustomRangeIncomplete) {
      setRangeError('Select both start and end date for custom range.');
      return;
    }

    if (isCustomRangeInvalid) {
      setRangeError('End date must be greater than or equal to start date.');
      return;
    }

    setRangeError('');
    if (loading) {
      setLoading(true);
    } else {
      setRefreshing(true);
    }

    const params = {};
    if (selectedRange) {
      params.range = selectedRange;
    }

    if (isCustomRange) {
      params.customStart = customStart;
      params.customEnd = customEnd;
    }

    try {
      const res = await axios.get(`${API_BASE_URL}/wear-events/stats/${userId}`, { params });
      setStats(normalizeStatsResponse(res.data));
    } catch (error) {
      const message = error.response?.data?.error || error.response?.data || 'Failed to load analytics.';
      setRangeError(String(message));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchStats({ manual: false });
  }, [userId, selectedRange]);

  useEffect(() => {
    if (!rangeError) {
      return;
    }

    setRangeError('');
  }, [customStart, customEnd]);

  const topStyle = useMemo(() => {
    const entries = Object.entries(stats.styleDist || {});
    if (entries.length === 0) {
      return null;
    }

    const [style, percentage] = entries.sort((a, b) => Number(b[1]) - Number(a[1]))[0];
    return {
      label: style,
      percentage: Number(percentage) || 0
    };
  }, [stats.styleDist]);

  const maxActivity = useMemo(
    () => Math.max(...stats.monthlyActivity.map((m) => m.total), 1),
    [stats.monthlyActivity]
  );

  if (loading) {
    return <div className="robotic-text" style={{ padding: '100px', textAlign: 'center', opacity: 0.5 }}>SYNCHRONIZING ANALYTICS...</div>;
  }

  const overviewCards = [
    { label: 'window', value: stats.window.label || 'all time' },
    { label: 'wear sessions', value: stats.totalWearSessions },
    { label: 'active days', value: stats.activeDays },
    { label: 'utilization', value: `${stats.utilizationRate.toFixed(0)}%` },
    { label: 'top style', value: topStyle ? `${topStyle.label} (${topStyle.percentage.toFixed(0)}%)` : 'n/a' }
  ];

  return (
    <div className="advanced-stats">
      <div className="stats-filter-bar">
        <div className="stats-filter-header">
          <span className="stats-filter-title">analytics window</span>
          {refreshing && <span className="refresh-badge">updating...</span>}
        </div>

        <div className="range-pills">
          {RANGE_OPTIONS.map((option) => (
            <button
              type="button"
              key={option.value}
              className={selectedRange === option.value ? 'active' : ''}
              onClick={() => setSelectedRange(option.value)}
              aria-label={`Set analytics range to ${option.label}`}
            >
              {option.label}
            </button>
          ))}
        </div>

        {isCustomRange && (
          <div className="custom-range-controls">
            <label className="date-label">
              <span>from</span>
              <input
                type="date"
                value={customStart}
                onChange={(e) => setCustomStart(e.target.value)}
              />
            </label>

            <label className="date-label">
              <span>to</span>
              <input
                type="date"
                value={customEnd}
                onChange={(e) => setCustomEnd(e.target.value)}
              />
            </label>

            <button
              type="button"
              className="apply-btn"
              onClick={() => fetchStats({ manual: true })}
              disabled={!canApplyCustomRange || refreshing}
              aria-label="Apply custom date range"
            >
              apply
            </button>

            {isCustomRangeInvalid && (
              <span className="range-hint">end date must be after start date</span>
            )}
          </div>
        )}

        {rangeError && <div className="range-error">{rangeError}</div>}
      </div>

      <div className="overview-cards-grid">
        {overviewCards.map((card) => (
          <div key={card.label} className="overview-card">
            <span className="overview-label">{card.label}</span>
            <span className="overview-value">{card.value}</span>
          </div>
        ))}
      </div>

      <div className="stats-tabs">
        {['overview', 'usage', 'style', 'outfits', 'timeline', 'diversity'].map((t) => (
          <button key={t} className={activeTab === t ? 'active' : ''} onClick={() => setActiveTab(t)}>{t}</button>
        ))}
      </div>

      <div className="tab-container">
        {activeTab === 'overview' && (
          <div className="stats-grid">
            <div className="stats-card">
              <h3>Streak</h3>
              <div className="streak-block">
                <div className="streak-item">
                  <span>Current streak</span>
                  <strong>{stats.streak.currentStreakDays} days</strong>
                </div>
                <div className="streak-item">
                  <span>Longest streak</span>
                  <strong>{stats.streak.longestStreakDays} days</strong>
                </div>
                <div className="streak-item">
                  <span>Latest wear</span>
                  <strong>{formatDate(stats.streak.latestWearDateUtc) || 'n/a'}</strong>
                </div>
              </div>
            </div>

            <div className="stats-card">
              <h3>AI vs custom</h3>
              <div className="split-row">
                <span>AI-generated</span>
                <strong>{stats.outfitSourceSplit.aiGeneratedPercentage.toFixed(0)}%</strong>
              </div>
              <div className="bar-bg"><div className="bar-fill" style={{ width: `${stats.outfitSourceSplit.aiGeneratedPercentage}%` }} /></div>

              <div className="split-row" style={{ marginTop: '18px' }}>
                <span>Custom</span>
                <strong>{stats.outfitSourceSplit.customPercentage.toFixed(0)}%</strong>
              </div>
              <div className="bar-bg"><div className="bar-fill custom" style={{ width: `${stats.outfitSourceSplit.customPercentage}%` }} /></div>
            </div>

            <div className="stats-card">
              <h3>Category utilization</h3>
              <div className="usage-list">
                {stats.categoryUtilization.length === 0 && <p className="empty-line">No category usage recorded in selected range.</p>}
                {stats.categoryUtilization.map((item) => (
                  <div key={item.category} className="style-row">
                    <div className="style-label">
                      <span>{item.category}</span>
                      <span>{item.utilizationRate.toFixed(0)}%</span>
                    </div>
                    <div className="bar-bg"><div className="bar-fill" style={{ width: `${item.utilizationRate}%` }} /></div>
                    <small style={{ color: '#999', marginTop: '6px', display: 'block' }}>{item.wornItems}/{item.totalItems} worn • {item.wearCount} wear events</small>
                  </div>
                ))}
              </div>
            </div>

            <div className="stats-card">
              <h3>Seasonal split</h3>
              <div className="seasonal-list">
                {stats.seasonalDist.length === 0 && <p className="empty-line">No seasonal data in selected range.</p>}
                {stats.seasonalDist.map((season) => (
                  <div key={season.season} className="season-row">
                    <span className="season-title">{season.season}</span>
                    <span className="season-count">{season.total} wears</span>
                    <span className="season-unique">{season.unique} pieces rotated</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'usage' && (
          <div className="stats-grid">
            <div className="stats-card">
              <h3>Top Most Worn</h3>
              <div className="usage-list">
                {stats.topWornItems.length === 0 && <p className="empty-line">No worn items in selected range.</p>}
                {stats.topWornItems.map((item) => (
                  <div key={item.id || item.name} className="usage-row">
                    <img src={item.imageUrl} alt={item.name} className="mini-thumb" />
                    <div className="usage-info">
                      <span className="item-name">{item.name}</span>
                      <span className="wear-tag">{item.count} wears</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="stats-card">
              <h3>Forgotten Pieces</h3>
              <div className="usage-list">
                {stats.unwornRecently.length === 0 && <p className="empty-line">No forgotten items in selected range.</p>}
                {stats.unwornRecently.map((item) => (
                  <div key={item.id || item.name} className="usage-row">
                    <img src={item.imageUrl} alt={item.name} className="mini-thumb" />
                    <div className="usage-info">
                      <span className="item-name">{item.name}</span>
                      <span className="alert-tag">Last worn {item.days} days ago</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'style' && (
          <div className="stats-grid">
            <div className="stats-card">
              <div className="insight-banner">{stats.colorInsight || 'Build more color variety by rotating underused tones.'}</div>
              <h3>Color Mix</h3>
              {stats.wornColors.length === 0 && <p className="empty-line">No color distribution available for this range.</p>}
              {stats.wornColors.map((c) => (
                <div key={c.color} className="color-bar-row">
                  <div className="color-swatch" style={{ background: getCssColor(c.color) }} />
                  <div className="color-meta">
                    <div className="color-label"><span>{c.color}</span><span>{c.pct.toFixed(0)}%</span></div>
                    <div className="bar-bg"><div className="bar-fill" style={{ width: `${c.pct}%`, background: getCssColor(c.color) }} /></div>
                  </div>
                </div>
              ))}
            </div>

            <div className="stats-card">
              <h3>Style Persona</h3>
              <div className="usage-list">
                {Object.keys(stats.styleDist).length === 0 && <p className="empty-line">No style data in selected range.</p>}
                {Object.entries(stats.styleDist).map(([s, p]) => (
                  <div key={s} className="style-row">
                    <div className="style-label"><span>{s.toUpperCase()}</span><span>{Number(p).toFixed(0)}%</span></div>
                    <div className="bar-bg"><div className="bar-fill" style={{ width: `${p}%`, background: '#000' }} /></div>
                  </div>
                ))}
              </div>
            </div>

            <div className="stats-card">
              <h3>Weekly vibes</h3>
              <div className="day-grid">
                {Object.keys(stats.styleByDay).length === 0 && <p className="empty-line">No weekly style pattern available.</p>}
                {Object.entries(stats.styleByDay).map(([day, style]) => (
                  <div key={day} className="day-box">
                    <span className="day-label">{day.slice(0, 3)}</span>
                    <span className="day-style">{style}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'outfits' && (
          <div className="stats-grid">
            <div className="stats-card">
              <h3>Favorite Outfits</h3>
              <p style={{ fontSize: '0.65rem', color: '#ccc', marginBottom: '20px' }}>Tap to view items</p>
              <div className="usage-list">
                {stats.topOutfits.length === 0 && <p className="empty-line">No outfit wear sessions in selected range.</p>}
                {stats.topOutfits.map((o) => (
                  <div key={o.id || o.name} className="usage-row clickable" onClick={() => setPreviewOutfit(o)}>
                    <div className="usage-info">
                      <span className="item-name">{o.name}</span>
                      <span className="wear-tag">{o.count} days worn</span>
                    </div>
                    <div className="mini-thumbs-preview">
                      {o.images.slice(0, 3).map((img, idx) => (
                        <img key={idx} src={img} alt={`${o.name} item ${idx + 1}`} style={{ width: '24px', height: '24px', borderRadius: '50%', border: '1px solid #fff', marginLeft: idx > 0 ? '-10px' : '0', objectFit: 'cover' }} />
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="stats-card">
              <h3>Monthly Activity</h3>
              <div className="activity-chart">
                {stats.monthlyActivity.length === 0 && <p className="empty-line">No monthly activity for selected range.</p>}
                {stats.monthlyActivity.map((m) => (
                  <div key={m.month} className="v-bar-container">
                    <div className="v-bar" style={{ height: `${(m.total / maxActivity) * 100}%`, background: '#000' }} title={`${m.total} outfit wears`} />
                    <span className="v-label">{m.month.split(' ')[0]}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'timeline' && (
          <div className="stats-grid">
            <div className="stats-card">
              <h3>Wear Timeline</h3>
              <div className="timeline-list">
                {stats.wearHistory.length === 0 && <p className="empty-line">No wear history in selected window.</p>}
                {stats.wearHistory.map((day) => (
                  <div key={day.date} className="timeline-day">
                    <div className="timeline-day-header">
                      <strong>{formatDate(day.date)}</strong>
                      <span>{day.outfits.length} session(s)</span>
                    </div>
                    <div className="timeline-sessions">
                      {day.outfits.map((session, idx) => (
                        <div key={`${day.date}-${idx}`} className="timeline-session">
                          <div className="timeline-meta">
                            <span>{session.outfitName || 'Custom Look'}</span>
                            <span>{formatTime(session.exactTime)}</span>
                          </div>
                          <div className="timeline-images">
                            {session.itemImages.slice(0, 5).map((img, imageIndex) => (
                              <img key={imageIndex} src={img} alt={`${session.outfitName || 'Outfit'} item ${imageIndex + 1}`} />
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'diversity' && (
          <div className="diversity-score-card">
            <div className="score-circle">
              <svg viewBox="0 0 36 36" className="circular-chart">
                <path className="circle-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <path className="circle" strokeDasharray={`${stats.utilizationRate}, 100`} d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <text x="18" y="20.35" className="percentage">{stats.utilizationRate.toFixed(0)}%</text>
              </svg>
            </div>
            <h2>UTILIZATION</h2>
            <p>{stats.diversityInsight || 'Rotate more wardrobe pieces to boost diversity.'}</p>
            <div className="diversity-meta">
              <span>{stats.totalDistinctWornItems} distinct worn items</span>
              <span>{stats.totalWearEvents} wear events</span>
            </div>
          </div>
        )}
      </div>

      <Modal isOpen={!!previewOutfit} onClose={() => setPreviewOutfit(null)} title={previewOutfit?.name} size="medium">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '15px', padding: '20px' }}>
          {previewOutfit?.images.map((img, i) => (
            <div key={i} style={{ textAlign: 'center', background: '#fcfcfc', borderRadius: '15px', padding: '10px', border: '1px solid #f5f5f5' }}>
              <img src={img} alt={`${previewOutfit?.name || 'Outfit'} preview ${i + 1}`} style={{ width: '100%', height: '160px', borderRadius: '10px', objectFit: 'contain' }} />
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
};

function normalizeStatsResponse(data) {
  const v = (obj, key) => {
    if (!obj) return null;
    const target = key.toLowerCase();
    const actualKey = Object.keys(obj).find((k) => k.toLowerCase() === target);
    return actualKey ? obj[actualKey] : null;
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

function parseMonthLabel(value) {
  if (!value) return 0;
  const parsed = new Date(`01 ${value}`);
  return Number.isNaN(parsed.getTime()) ? 0 : parsed.getTime();
}

function getSeasonRank(value) {
  if (!value) return Number.MAX_SAFE_INTEGER;
  const index = SEASON_ORDER.indexOf(String(value).toLowerCase());
  return index === -1 ? Number.MAX_SAFE_INTEGER : index;
}

function getCssColor(c) {
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
  return map[c?.toLowerCase()] || c || '#999';
}

function formatDate(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toLocaleDateString();
}

function formatTime(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export default StatsSection;
