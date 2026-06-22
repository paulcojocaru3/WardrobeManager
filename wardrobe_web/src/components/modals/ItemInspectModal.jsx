import { useState, useEffect } from 'react';
import Modal from '../Modal';
import { CLOTHING_TYPES, SEASONS, USAGES } from '../../constants/wardrobe';
import { colorToHex, COLOR_HEX } from '../../constants/colors';
import { toStringArray } from '../../utils/wardrobeTransforms';
import { clothingApi } from '../../services/wardrobeApi';

const COLOR_NAMES = Object.keys(COLOR_HEX);

// Swatch style for one color name: resolved hex, or a neutral ring when unknown.
const dotStyle = (name) => {
  const hex = colorToHex(name);
  return hex
    ? { background: hex }
    : { background: 'transparent', boxShadow: 'inset 0 0 0 1px var(--border-muted, #999)' };
};

const ItemInspectModal = ({
  isOpen, onClose,
  selectedItem, editItemMode, setEditItemMode,
  editItemData, setEditItemData,
  subtypeOptions = {},
  onUpdateItem, onGenerate, loading,
  onSelectSimilar
}) => {
  const [colorSearch, setColorSearch] = useState('');
  // Results are tagged with the item they belong to, so a previous item's matches
  // never render against a newly-opened one (and we avoid setState in the effect body).
  const [similar, setSimilar] = useState({ forId: null, items: [] });

  // Fetch visually-closest wardrobe items whenever a new item is inspected (view mode only).
  // The strip is only rendered in view mode, so we simply skip fetching while editing.
  const itemId = selectedItem?.id;
  useEffect(() => {
    if (!itemId || editItemMode) return;
    let cancelled = false;
    clothingApi.getSimilar(itemId, { limit: 6 })
      .then(res => { if (!cancelled) setSimilar({ forId: itemId, items: Array.isArray(res.data) ? res.data : [] }); })
      .catch(() => { if (!cancelled) setSimilar({ forId: itemId, items: [] }); });
    return () => { cancelled = true; };
  }, [itemId, editItemMode]);

  const similarItems = similar.forId === itemId ? similar.items : [];

  if (!selectedItem) return null;

  const typeStr = typeof selectedItem.type === 'number'
    ? CLOTHING_TYPES[selectedItem.type]
    : selectedItem.type;

  // Sub-type options for the type currently chosen in the edit form (live ML vocabulary).
  const editTypeName = editItemData
    ? (typeof editItemData.type === 'number' ? CLOTHING_TYPES[editItemData.type] : editItemData.type)
    : null;
  const baseSubOptions = (editTypeName && subtypeOptions[editTypeName]) || [];
  // Keep the current value selectable even if it isn't in the model's list for this type.
  const subOptions = editItemData?.subType && !baseSubOptions.includes(editItemData.subType)
    ? [editItemData.subType, ...baseSubOptions]
    : baseSubOptions;

  const usageTags = (selectedItem.usage || 'Casual')
    .split(',').map(u => u.trim()).filter(Boolean);

  const seasonStr = selectedItem.season || 'Any';

  // A garment can have several colors (CSV like Season/Usage).
  const colorList = toStringArray(selectedItem.color);

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="large">
      <div className="iim-root">

        {/* ── Left: image panel ── */}
        <div className="iim-img-panel">
          <div className="iim-img-frame">
            <img src={selectedItem.processedImageUrl} alt={selectedItem.name} />
          </div>
          <div className="iim-img-chips">
            <div className="iim-img-chip">
              <span className="iim-chip-lbl">Type</span>
              <span className="iim-chip-val">{typeStr || '—'}</span>
            </div>
            <div className="iim-img-chip">
              <span className="iim-chip-lbl">Color</span>
              <span className="iim-chip-val iim-chip-color">
                {colorList.length > 0
                  ? colorList.map(c => <span key={c} className="iim-color-dot" style={dotStyle(c)} title={c} />)
                  : <span className="iim-color-dot" style={dotStyle(null)} />}
                {selectedItem.color || '—'}
              </span>
            </div>
          </div>
        </div>

        {/* ── Right: detail / edit panel ── */}
        <div className="iim-detail">

          <div className="iim-name-block">
            <div className="iim-name-row">
              <h2 className="iim-name">{selectedItem.name}</h2>
              {typeStr && <span className="iim-type-badge">{typeStr}</span>}
            </div>
            <span className="iim-sub">
              {selectedItem.color ? selectedItem.color : ''}
            </span>
          </div>

          <div className="iim-divider" />

          {editItemMode ? (
            <div className="iim-form">
              <div className="iim-form-grid">

                <div className="iim-field full">
                  <label className="iim-field-label">Name</label>
                  <input
                    value={editItemData.name}
                    onChange={e => setEditItemData({ ...editItemData, name: e.target.value })}
                    placeholder="Item name…"
                  />
                </div>

                <div className="iim-field">
                  <label className="iim-field-label">Type</label>
                  <select
                    value={typeof editItemData.type === 'number' ? CLOTHING_TYPES[editItemData.type] : editItemData.type}
                    onChange={e => setEditItemData({ ...editItemData, type: CLOTHING_TYPES.indexOf(e.target.value) })}
                  >
                    {CLOTHING_TYPES.map(t => <option key={t}>{t}</option>)}
                  </select>
                </div>

                <div className="iim-field">
                  <label className="iim-field-label">Sub-type</label>
                  <select
                    value={editItemData.subType || ''}
                    onChange={e => setEditItemData({ ...editItemData, subType: e.target.value || null })}
                    style={{ textTransform: 'capitalize' }}
                  >
                    <option value="">—</option>
                    {subOptions.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>

                <div className="iim-field full">
                  <label className="iim-field-label">Colors</label>

                  {/* Currently selected — always visible, click × to remove. */}
                  {editItemData.color.length > 0 && (
                    <div className="iim-toggle-group" style={{ marginBottom: '8px' }}>
                      {editItemData.color.map(c => (
                        <button
                          key={c}
                          type="button"
                          className="iim-toggle on"
                          onClick={() => setEditItemData({ ...editItemData, color: editItemData.color.filter(x => x !== c) })}
                        >
                          <span className="iim-color-dot" style={{ ...dotStyle(c), marginRight: 6 }} />
                          {c} ×
                        </button>
                      ))}
                    </div>
                  )}

                  <input
                    value={colorSearch}
                    onChange={e => setColorSearch(e.target.value)}
                    placeholder="search to add a color…"
                  />

                  {/* Options appear only while searching, so the modal stays compact. */}
                  {colorSearch.trim() && (
                    <div className="iim-toggle-group" style={{ marginTop: '8px' }}>
                      {COLOR_NAMES
                        .filter(n => !editItemData.color.includes(n) && n.toLowerCase().includes(colorSearch.trim().toLowerCase()))
                        .map(n => (
                          <button
                            key={n}
                            type="button"
                            className="iim-toggle"
                            onClick={() => {
                              setEditItemData({ ...editItemData, color: [...editItemData.color, n] });
                              setColorSearch('');
                            }}
                          >
                            <span className="iim-color-dot" style={{ ...dotStyle(n), marginRight: 6 }} />
                            {n}
                          </button>
                        ))}
                    </div>
                  )}
                </div>

                <div className="iim-field full">
                  <label className="iim-field-label">Season</label>
                  <div className="iim-toggle-group">
                    {SEASONS.map(s => {
                      const on = editItemData.season.includes(s);
                      const isAll = s === 'All Seasons';
                      return (
                        <button
                          key={s}
                          className={`iim-toggle${on ? ' on' : ''}`}
                          onClick={() => {
                            let next;
                            if (isAll) {
                              // "All Seasons" is exclusive: selecting it clears the rest.
                              next = on ? [] : ['All Seasons'];
                            } else {
                              next = on
                                ? editItemData.season.filter(x => x !== s)
                                : [...editItemData.season.filter(x => x !== 'All Seasons'), s];
                            }
                            setEditItemData({ ...editItemData, season: next });
                          }}
                        >{s}</button>
                      );
                    })}
                  </div>
                </div>

                <div className="iim-field full">
                  <label className="iim-field-label">Usage / Style</label>
                  <div className="iim-toggle-group">
                    {USAGES.map(u => {
                      const on = editItemData.usage.includes(u);
                      return (
                        <button
                          key={u}
                          className={`iim-toggle${on ? ' on' : ''}`}
                          onClick={() => {
                            const next = on
                              ? editItemData.usage.filter(x => x !== u)
                              : [...editItemData.usage, u];
                            setEditItemData({ ...editItemData, usage: next });
                          }}
                        >{u}</button>
                      );
                    })}
                  </div>
                </div>

              </div>

              <div className="iim-actions">
                <button className="sw-btn accent" onClick={onUpdateItem} disabled={loading} style={{ flex: 2 }}>
                  {loading ? 'Saving…' : 'Save changes'}
                </button>
                <button className="sw-btn ghost" onClick={() => setEditItemMode(false)} style={{ flex: 1 }}>
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="iim-attrs-grid">
                <div className="iim-attr-card">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Type</span>
                    <span className="iim-attr-value">{typeStr || '—'}</span>
                  </div>
                </div>

                <div className="iim-attr-card">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M4 7h16M4 12h16M4 17h10"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Sub-type</span>
                    <span className="iim-attr-value" style={{ textTransform: 'capitalize' }}>{selectedItem.subType || '—'}</span>
                  </div>
                </div>

                <div className="iim-attr-card">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" stroke="none"><circle cx="12" cy="12" r="8"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Color</span>
                    <span className="iim-attr-value iim-chip-color">
                      {colorList.length > 0
                        ? colorList.map(c => <span key={c} className="iim-color-dot" style={dotStyle(c)} title={c} />)
                        : <span className="iim-color-dot" style={dotStyle(null)} />}
                      {selectedItem.color || '—'}
                    </span>
                  </div>
                </div>

                <div className="iim-attr-card">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Added</span>
                    <span className="iim-attr-value">
                      {selectedItem.createdAt
                        ? new Date(selectedItem.createdAt).toLocaleDateString()
                        : '—'}
                    </span>
                  </div>
                </div>

                <div className="iim-attr-card iim-attr-card-wide">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2z"/><path d="M12 6v6l4 2"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Season</span>
                    <span className="iim-attr-value">{seasonStr}</span>
                  </div>
                </div>

                <div className="iim-attr-card iim-attr-card-full">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"/><path d="M12 1v4M12 19v4M4.22 4.22l2.83 2.83M16.95 16.95l2.83 2.83M1 12h4M19 12h4M4.22 19.78l2.83-2.83M16.95 7.05l2.83-2.83"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Usage / Style</span>
                    <div className="iim-attr-tags">
                      {usageTags.map(u => (
                        <span key={u} className="sw-tag">{u}</span>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              <div className="iim-actions">
                <button
                  className="sw-btn accent"
                  onClick={() => onGenerate(selectedItem)}
                  disabled={loading}
                  style={{ flex: 2 }}
                >
                  {loading ? 'Generating…' : 'Generate outfit'}
                </button>
                <button
                  className="sw-btn ghost"
                  onClick={() => {
                    setEditItemData({
                      ...selectedItem,
                      color: toStringArray(selectedItem.color),
                      season: toStringArray(selectedItem.season),
                      usage: toStringArray(selectedItem.usage),
                    });
                    setEditItemMode(true);
                  }}
                  style={{ flex: 1 }}
                >
                  Edit
                </button>
              </div>

              {similarItems.length > 0 && (
                <div className="iim-similar">
                  <div className="iim-similar-head">More like this</div>
                  <div className="iim-similar-strip">
                    {similarItems.map(({ item, similarity }) => (
                      <button
                        key={item.id}
                        type="button"
                        className="iim-similar-card"
                        onClick={() => onSelectSimilar?.(item)}
                        title={`${item.name} · ${Math.round((similarity ?? 0) * 100)}% match`}
                      >
                        <img src={item.processedImageUrl} alt={item.name} />
                        <span className="iim-similar-score">{Math.round((similarity ?? 0) * 100)}%</span>
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </Modal>
  );
};

export default ItemInspectModal;
