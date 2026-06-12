import React, { useEffect, useMemo, useState } from 'react';
import Modal from '../components/Modal';
import './StatsSection.css';
import { statsApi } from '../services/wardrobeApi';
import { getErrorMessage } from '../utils/errors';
import { formatDate, formatTime, getCssColor, normalizeStatsResponse } from '../utils/wardrobeTransforms';

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

const TABS = ['overview', 'usage', 'style', 'outfits', 'timeline', 'diversity'];

const prefersReducedMotion = () =>
  typeof window !== 'undefined' && window.matchMedia
    ? window.matchMedia('(prefers-reduced-motion: reduce)').matches
    : false;

// Animate a number from 0 → target on mount / when target changes.
const useCountUp = (target, duration = 750) => {
  const [val, setVal] = useState(target);
  useEffect(() => {
    if (prefersReducedMotion()) {
      const id = requestAnimationFrame(() => setVal(target));
      return () => cancelAnimationFrame(id);
    }
    let raf;
    const start = performance.now();
    const tick = (now) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      setVal(target * eased);
      if (t < 1) raf = requestAnimationFrame(tick);
      else setVal(target);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, duration]);
  return val;
};

// Pick legible label color (black/white) for a swatch background.
const textOnColor = (hex) => {
  if (typeof hex !== 'string' || hex[0] !== '#') return '#fff';
  let h = hex.slice(1);
  if (h.length === 3) h = h.split('').map((c) => c + c).join('');
  if (h.length !== 6) return '#fff';
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  const lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return lum > 0.62 ? '#161514' : '#ffffff';
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

  const fetchStats = async () => {
    if (!userId) return;
    if (isCustomRangeIncomplete) { setRangeError('Select both start and end date.'); return; }
    if (isCustomRangeInvalid) { setRangeError('End date must be after start date.'); return; }
    setRangeError('');
    setRefreshing(true);
    const params = { range: selectedRange };
    if (isCustomRange) { params.customStart = customStart; params.customEnd = customEnd; }
    try {
      const res = await statsApi.getWearStats(userId, params);
      setStats(normalizeStatsResponse(res.data));
    } catch (error) {
      setRangeError(getErrorMessage(error, 'Failed to load analytics.'));
      setStats(DEFAULT_STATS);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    if (!userId || selectedRange === 'custom') return;
    const run = async () => {
      setRangeError('');
      setRefreshing(true);
      try {
        const res = await statsApi.getWearStats(userId, { range: selectedRange });
        setStats(normalizeStatsResponse(res.data));
      } catch (error) {
        setRangeError(getErrorMessage(error, 'Failed to load analytics.'));
        setStats(DEFAULT_STATS);
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    };
    run();
  }, [userId, selectedRange]);

  const topStyle = useMemo(() => {
    const entries = Object.entries(stats.styleDist || {});
    if (!entries.length) return null;
    const [style, percentage] = entries.sort((a, b) => Number(b[1]) - Number(a[1]))[0];
    return { label: style, percentage: Number(percentage) || 0 };
  }, [stats.styleDist]);

  const maxActivity = useMemo(
    () => Math.max(...stats.monthlyActivity.map((m) => m.total), 1),
    [stats.monthlyActivity]
  );

  const heatmapCells = useMemo(() => {
    const dateMap = new Map(
      stats.wearHistory.map(d => [d.date.split('T')[0], d.outfits.length])
    );
    const today = new Date();
    const dow = today.getDay();
    const monday = new Date(today);
    monday.setDate(today.getDate() - (dow === 0 ? 6 : dow - 1));
    const start = new Date(monday);
    start.setDate(monday.getDate() - 42);
    const cells = [];
    for (let i = 0; i < 49; i++) {
      const d = new Date(start);
      d.setDate(start.getDate() + i);
      const dateStr = d.toISOString().split('T')[0];
      const count = dateMap.get(dateStr) || 0;
      const isFuture = d > today;
      const level = isFuture ? -1 : count === 0 ? 0 : count === 1 ? 1 : count === 2 ? 2 : count <= 4 ? 3 : 4;
      cells.push({ dateStr, count, level });
    }
    return cells;
  }, [stats.wearHistory]);

  const heroPct = Math.round(useCountUp(stats.utilizationRate || 0));
  const hasRibbon = stats.wornColors.length > 0;

  if (loading) {
    return (
      <div className="st-root">
        <div className="st-sk st-sk-filter" />
        <div className="st-sk st-sk-hero" />
        <div className="st-sk st-sk-ribbon" />
        <div className="st-sk st-sk-tabs" />
        <div className="st-asym">
          <div className="st-col">
            <div className="st-sk st-sk-card" />
            <div className="st-sk st-sk-card" />
          </div>
          <div className="st-col">
            <div className="st-sk st-sk-card sm" />
            <div className="st-sk st-sk-card sm" />
          </div>
        </div>
      </div>
    );
  }

  const renderHeatmap = () => (
    <div className="st-card">
      <div className="st-card-hd">
        <h3>Wear frequency</h3>
        <div className="st-grow" />
        <span className="st-meta">Last 7 weeks</span>
      </div>
      <div className="st-heatmap">
        <div />
        {['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((d, i) => (
          <div key={i} className="st-hm-label st-hm-label-center">{d}</div>
        ))}
        {Array.from({ length: 7 }).map((_, w) => (
          <React.Fragment key={w}>
            <div className="st-hm-label" />
            {Array.from({ length: 7 }).map((_, d) => {
              const cell = heatmapCells[w * 7 + d];
              const lvl = cell?.level ?? 0;
              return (
                <div
                  key={d}
                  className={`st-hm-day${lvl === -1 ? ' future' : ''}`}
                  data-level={lvl > 0 ? lvl : undefined}
                  title={cell && lvl >= 0 ? `${cell.dateStr}: ${cell.count} outfit${cell.count !== 1 ? 's' : ''}` : ''}
                />
              );
            })}
          </React.Fragment>
        ))}
      </div>
      <div className="st-hm-legend">
        <span className="st-mono">less</span>
        {[0, 1, 2, 3, 4].map((l) => (
          <span key={l} className="st-hm-swatch" data-level={l || undefined} />
        ))}
        <span className="st-mono">more</span>
      </div>
    </div>
  );

  const renderAiVsCustom = () => (
    <div className="st-card">
      <div className="st-card-hd">
        <h3>AI vs. custom</h3>
        <div className="st-grow" />
        <span className="st-meta">{stats.outfitSourceSplit.totalSessions} sessions</span>
      </div>
      <div className="st-split-row">
        <span>AI generated</span>
        <strong>{stats.outfitSourceSplit.aiGeneratedPercentage.toFixed(0)}%</strong>
      </div>
      <div className="st-bar-bg"><span className="st-bar-fill" style={{ width: `${stats.outfitSourceSplit.aiGeneratedPercentage}%` }} /></div>
      <div className="st-split-row" style={{ marginTop: 18 }}>
        <span>Custom</span>
        <strong>{stats.outfitSourceSplit.customPercentage.toFixed(0)}%</strong>
      </div>
      <div className="st-bar-bg"><span className="st-bar-fill st-bar-alt" style={{ width: `${stats.outfitSourceSplit.customPercentage}%` }} /></div>
    </div>
  );

  const renderStreak = () => (
    <div className="st-card">
      <div className="st-card-hd"><h3>Streak</h3></div>
      <div className="st-info-list">
        <div className="st-info-row"><span>Current streak</span><span className="st-info-val">{stats.streak.currentStreakDays} days</span></div>
        <div className="st-info-row"><span>Longest streak</span><span className="st-info-val">{stats.streak.longestStreakDays} days</span></div>
        <div className="st-info-row"><span>Latest wear</span><span className="st-info-val">{formatDate(stats.streak.latestWearDateUtc) || 'n/a'}</span></div>
      </div>
    </div>
  );

  return (
    <div className="st-root">

      {/* Filter bar */}
      <div className="st-filter">
        <div className="st-filter-left">
          <span className="st-mono">analytics window</span>
          {refreshing && <span className="st-updating">updating…</span>}
        </div>
        <div className="st-range-seg">
          {RANGE_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              className={selectedRange === opt.value ? 'on' : ''}
              onClick={() => { setRangeError(''); setSelectedRange(opt.value); }}
            >
              {opt.label}
            </button>
          ))}
        </div>
        {isCustomRange && (
          <div className="st-custom-range">
            <label className="st-date-field">
              <span className="st-mono">from</span>
              <input type="date" value={customStart} onChange={(e) => { setRangeError(''); setCustomStart(e.target.value); }} />
            </label>
            <label className="st-date-field">
              <span className="st-mono">to</span>
              <input type="date" value={customEnd} onChange={(e) => { setRangeError(''); setCustomEnd(e.target.value); }} />
            </label>
            <button className="st-apply-btn" onClick={fetchStats} disabled={!canApplyCustomRange || refreshing}>apply</button>
            {isCustomRangeInvalid && <span className="st-range-hint">end must be after start</span>}
          </div>
        )}
        {rangeError && <div className="st-error">{rangeError}</div>}
      </div>

      {/* Hero band */}
      <div className={`st-hero${hasRibbon ? ' has-ribbon' : ''}`}>
        <div className="st-hero-lead">
          <div className="st-hero-eyebrow">closet in motion · {stats.window.label || 'all time'}</div>
          <div className="st-hero-num">{heroPct}<small>%</small></div>
          <div className="st-hero-sub">
            {stats.totalDistinctWornItems} {stats.totalDistinctWornItems === 1 ? 'piece' : 'pieces'} worn across {stats.activeDays} active {stats.activeDays === 1 ? 'day' : 'days'}.
          </div>
        </div>
        <div className="st-hero-kpis">
          <div className="st-hk"><span className="st-mono">sessions</span><span className="st-hk-n">{stats.totalWearSessions}</span></div>
          <div className="st-hk"><span className="st-mono">active days</span><span className="st-hk-n">{stats.activeDays}</span></div>
          <div className="st-hk"><span className="st-mono">streak</span><span className="st-hk-n">{stats.streak.currentStreakDays}</span></div>
          {topStyle && <div className="st-hk"><span className="st-mono">top style</span><span className="st-hk-n word">{topStyle.label}</span></div>}
        </div>
      </div>

      {/* Signature color ribbon */}
      {hasRibbon && (
        <div className="st-ribbon">
          <div className="st-ribbon-bar">
            {stats.wornColors.map((c) => {
              const hex = getCssColor(c.color);
              return (
                <span
                  key={c.color}
                  style={{ background: hex, flexGrow: c.pct, color: textOnColor(hex) }}
                  title={`${c.color} · ${c.pct.toFixed(0)}%`}
                >
                  {c.pct >= 8 && <span className="st-ribbon-lab">{c.color}<br />{c.pct.toFixed(0)}%</span>}
                </span>
              );
            })}
          </div>
          <div className="st-ribbon-foot">
            <span className="st-mono">color palette · by wear frequency</span>
            <span className="st-mono">{stats.wornColors.length} colors worn</span>
          </div>
        </div>
      )}

      {/* Tab navigation */}
      <div className="st-tabs-seg">
        {TABS.map((t) => (
          <button key={t} className={activeTab === t ? 'on' : ''} onClick={() => setActiveTab(t)}>{t}</button>
        ))}
      </div>

      {/* Tab content (keyed so it re-animates on switch) */}
      <div className="st-tab-body" key={activeTab}>

        {/* ── OVERVIEW ── */}
        {activeTab === 'overview' && (
          <div className="st-stack">
            <div className="st-asym">
              <div className="st-col">
                {renderHeatmap()}

                {/* Category utilization */}
                <div className="st-card">
                  <div className="st-card-hd">
                    <h3>Category utilization</h3>
                    <div className="st-grow" />
                    <span className="st-meta">{stats.categoryUtilization.length} categories</span>
                  </div>
                  {stats.categoryUtilization.length === 0
                    ? <p className="st-empty">No category data in selected range.</p>
                    : (
                      <div className="st-bar-list">
                        {stats.categoryUtilization.map((item) => (
                          <div key={item.category}>
                            <div className="st-bar-row">
                              <span className="st-bar-nm">{item.category}</span>
                              <div className="st-bar-bg"><span className="st-bar-fill" style={{ width: `${item.utilizationRate}%` }} /></div>
                              <span className="st-bar-v">{item.utilizationRate.toFixed(0)}%</span>
                            </div>
                            <div className="st-bar-sub">{item.wornItems}/{item.totalItems} worn · {item.wearCount} wears</div>
                          </div>
                        ))}
                      </div>
                    )
                  }
                </div>
              </div>

              <div className="st-col">
                {renderAiVsCustom()}
                {renderStreak()}
              </div>
            </div>

            {/* Seasonal split (full width) */}
            <div className="st-card">
              <div className="st-card-hd"><h3>Seasonal split</h3></div>
              {stats.seasonalDist.length === 0
                ? <p className="st-empty">No seasonal data in selected range.</p>
                : (
                  <div className="st-season-grid">
                    {stats.seasonalDist.map((s) => (
                      <div key={s.season} className="st-season-cell">
                        <span className="st-season-name">{s.season}</span>
                        <span className="st-season-n">{s.total}</span>
                        <span className="st-mono">{s.unique} pieces</span>
                      </div>
                    ))}
                  </div>
                )
              }
            </div>
          </div>
        )}

        {/* ── USAGE ── */}
        {activeTab === 'usage' && (
          <div className="st-2col">
            {/* Most worn */}
            <div className="st-card">
              <div className="st-card-hd">
                <h3>Most worn</h3>
                <div className="st-grow" />
                <span className="st-meta">Top {stats.topWornItems.length}</span>
              </div>
              {stats.topWornItems.length === 0
                ? <p className="st-empty">No worn items in selected range.</p>
                : stats.topWornItems.map((item, i) => (
                  <div key={item.id || item.name} className="st-mw-row">
                    <span className="st-mw-rank">#{i + 1}</span>
                    <div className="st-mw-img">
                      <img src={item.imageUrl} alt={item.name} />
                    </div>
                    <div className="st-mw-meta">
                      <div className="st-mw-name">{item.name}</div>
                      <div className="st-mono">{item.count} wears</div>
                    </div>
                    <div className="st-mw-count">
                      {item.count}
                      <small>wears</small>
                    </div>
                  </div>
                ))
              }
            </div>

            {/* Forgotten pieces */}
            <div className="st-card">
              <div className="st-card-hd">
                <h3>Forgotten pieces</h3>
                <div className="st-grow" />
                <span className="st-meta">{stats.unwornRecently.length} items</span>
              </div>
              {stats.unwornRecently.length === 0
                ? <p className="st-empty">No forgotten items in selected range.</p>
                : (
                  <>
                    <p className="st-desc">These pieces haven't been worn recently. Time to bring them back.</p>
                    <div className="st-under-grid">
                      {stats.unwornRecently.map((item) => (
                        <div key={item.id || item.name} className="st-u-cell">
                          <div className="st-u-img"><img src={item.imageUrl} alt={item.name} /></div>
                          <div className="st-u-name">{item.name}</div>
                          <div className="st-mono">{item.days}d ago</div>
                        </div>
                      ))}
                    </div>
                  </>
                )
              }
            </div>
          </div>
        )}

        {/* ── STYLE ── */}
        {activeTab === 'style' && (
          <div className="st-stack">
            <div className="st-2col">
              {/* Style persona */}
              <div className="st-card">
                <div className="st-card-hd"><h3>Style persona</h3></div>
                {Object.keys(stats.styleDist).length === 0
                  ? <p className="st-empty">No style data in selected range.</p>
                  : (
                    <div className="st-bar-list">
                      {Object.entries(stats.styleDist).map(([s, p]) => (
                        <div key={s} className="st-bar-row">
                          <span className="st-bar-nm">{s.toUpperCase()}</span>
                          <div className="st-bar-bg"><span className="st-bar-fill" style={{ width: `${p}%` }} /></div>
                          <span className="st-bar-v">{Number(p).toFixed(0)}%</span>
                        </div>
                      ))}
                    </div>
                  )
                }
              </div>

              {/* Weekly vibes */}
              <div className="st-card">
                <div className="st-card-hd"><h3>Weekly vibes</h3></div>
                {Object.keys(stats.styleByDay).length === 0
                  ? <p className="st-empty">No weekly style pattern available.</p>
                  : (
                    <div className="st-day-grid">
                      {Object.entries(stats.styleByDay).map(([day, style]) => (
                        <div key={day} className="st-day-cell">
                          <span className="st-mono">{day.slice(0, 3)}</span>
                          <span className="st-day-style">{style}</span>
                        </div>
                      ))}
                    </div>
                  )
                }
              </div>
            </div>

            {stats.colorInsight && (
              <div className="st-card">
                <div className="st-card-hd"><h3>Color story</h3></div>
                <p className="st-insight">{stats.colorInsight}</p>
              </div>
            )}
          </div>
        )}

        {/* ── OUTFITS ── */}
        {activeTab === 'outfits' && (
          <div className="st-2col">
            {/* Favorite outfits */}
            <div className="st-card">
              <div className="st-card-hd">
                <h3>Favorite outfits</h3>
                <div className="st-grow" />
                <span className="st-meta">Tap to preview</span>
              </div>
              {stats.topOutfits.length === 0
                ? <p className="st-empty">No outfit wear sessions in selected range.</p>
                : stats.topOutfits.map((o) => (
                  <div key={o.id || o.name} className="st-mw-row clickable" onClick={() => setPreviewOutfit(o)}>
                    <div className="st-mw-meta">
                      <div className="st-mw-name">{o.name}</div>
                      <div className="st-mono">{o.count} days worn</div>
                    </div>
                    <div className="st-outfit-thumbs">
                      {o.images.slice(0, 3).map((img, idx) => (
                        <img key={idx} src={img} alt={`${o.name} item ${idx + 1}`} />
                      ))}
                    </div>
                    <div className="st-mw-count">
                      {o.count}
                      <small>days</small>
                    </div>
                  </div>
                ))
              }
            </div>

            {/* Monthly activity */}
            <div className="st-card">
              <div className="st-card-hd">
                <h3>Monthly activity</h3>
                <div className="st-grow" />
                <span className="st-meta">{stats.monthlyActivity.length} months</span>
              </div>
              {stats.monthlyActivity.length === 0
                ? <p className="st-empty">No monthly activity for selected range.</p>
                : (
                  <div className="st-activity-chart">
                    {stats.monthlyActivity.map((m) => (
                      <div key={m.month} className="st-v-bar-wrap">
                        <div
                          className="st-v-bar"
                          style={{ height: `${(m.total / maxActivity) * 100}%` }}
                          title={`${m.total} outfit wears`}
                        />
                        <span className="st-v-label">{m.month.split(' ')[0]}</span>
                      </div>
                    ))}
                  </div>
                )
              }
            </div>
          </div>
        )}

        {/* ── TIMELINE ── */}
        {activeTab === 'timeline' && (
          <div className="st-card">
            <div className="st-card-hd">
              <h3>Wear timeline</h3>
              <div className="st-grow" />
              <span className="st-meta">{stats.wearHistory.length} days</span>
            </div>
            {stats.wearHistory.length === 0
              ? <p className="st-empty">No wear history in selected window.</p>
              : (
                <div className="st-timeline">
                  {stats.wearHistory.map((day) => (
                    <div key={day.date} className="st-tl-day">
                      <div className="st-tl-header">
                        <strong>{formatDate(day.date)}</strong>
                        <span>{day.outfits.length} session{day.outfits.length !== 1 ? 's' : ''}</span>
                      </div>
                      <div className="st-tl-sessions">
                        {day.outfits.map((session, idx) => (
                          <div key={`${day.date}-${idx}`} className="st-tl-session">
                            <div className="st-tl-meta">
                              <span>{session.outfitName || 'Custom Look'}</span>
                              <span>{formatTime(session.exactTime)}</span>
                            </div>
                            <div className="st-tl-images">
                              {session.itemImages.slice(0, 5).map((img, ii) => (
                                <img key={ii} src={img} alt={`${session.outfitName || 'Outfit'} item ${ii + 1}`} />
                              ))}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )
            }
          </div>
        )}

        {/* ── DIVERSITY ── */}
        {activeTab === 'diversity' && (
          <div className="st-card st-div-grid">
            <div className="st-circ-wrap">
              <svg viewBox="0 0 36 36" className="st-circ">
                <path className="st-circ-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <path className="st-circ-fill" strokeDasharray={`${stats.utilizationRate}, 100`} d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <text x="18" y="20.35" className="st-circ-pct">{stats.utilizationRate.toFixed(0)}%</text>
              </svg>
            </div>
            <div className="st-div-body">
              <span className="st-mono">closet utilization</span>
              <h2 className="st-div-title">{stats.utilizationRate.toFixed(0)}% of your closet is in rotation</h2>
              <p className="st-div-insight">{stats.diversityInsight || 'Rotate more wardrobe pieces to boost diversity.'}</p>
              <div className="st-div-meta">
                <span>{stats.totalDistinctWornItems} distinct worn items</span>
                <span>{stats.totalWearEvents} wear events</span>
              </div>
            </div>
          </div>
        )}
      </div>

      <Modal isOpen={!!previewOutfit} onClose={() => setPreviewOutfit(null)} title={previewOutfit?.name} size="medium">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '15px', padding: '20px' }}>
          {previewOutfit?.images.map((img, i) => (
            <div key={i} style={{ textAlign: 'center', background: 'var(--bg-subtle)', borderRadius: '15px', padding: '10px', border: '1px solid var(--border-subtle)' }}>
              <img src={img} alt={`${previewOutfit?.name || 'Outfit'} preview ${i + 1}`} style={{ width: '100%', height: '160px', borderRadius: '10px', objectFit: 'contain' }} />
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
};

export default StatsSection;
