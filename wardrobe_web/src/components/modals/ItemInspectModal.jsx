import React from 'react';
import Modal from '../Modal';
import { CLOTHING_TYPES, COLORS, GENDERS, SEASONS, USAGES } from '../../constants/wardrobe';
import { toStringArray } from '../../utils/wardrobeTransforms';

const ItemInspectModal = ({
  isOpen, onClose,
  selectedItem, editItemMode, setEditItemMode,
  editItemData, setEditItemData,
  onUpdateItem, onGenerate, loading
}) => {
  if (!selectedItem) return null;

  const typeStr = typeof selectedItem.type === 'number'
    ? CLOTHING_TYPES[selectedItem.type]
    : selectedItem.type;

  const usageTags = (selectedItem.usage || 'Casual')
    .split(',').map(u => u.trim()).filter(Boolean);

  const seasonStr = selectedItem.season || 'Any';

  const colorHex = {
    black: '#111', white: '#f5f5f5', red: '#e05a5a', blue: '#4f8ef7',
    green: '#4caf7d', yellow: '#f5c842', purple: '#a855f7', pink: '#f472b6',
    orange: '#f97316', brown: '#92400e', grey: '#6b7280', gray: '#6b7280',
    navy: '#1e3a5f', beige: '#d4b896',
  };
  const colorKey = (selectedItem.color || '').toLowerCase().split(' ')[0];
  const swatchColor = colorHex[colorKey];

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
                {swatchColor && <span className="iim-color-dot" style={{ background: swatchColor }} />}
                {selectedItem.color || '—'}
              </span>
            </div>
            <div className="iim-img-chip">
              <span className="iim-chip-lbl">Gender</span>
              <span className="iim-chip-val">{selectedItem.gender || 'Unisex'}</span>
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
              {selectedItem.color && selectedItem.gender ? ' · ' : ''}
              {selectedItem.gender || ''}
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
                  <label className="iim-field-label">Color</label>
                  <select
                    value={editItemData.color}
                    onChange={e => setEditItemData({ ...editItemData, color: e.target.value })}
                  >
                    {COLORS.map(c => <option key={c}>{c}</option>)}
                  </select>
                </div>

                <div className="iim-field full">
                  <label className="iim-field-label">Gender</label>
                  <select
                    value={editItemData.gender}
                    onChange={e => setEditItemData({ ...editItemData, gender: e.target.value })}
                  >
                    {GENDERS.map(g => <option key={g}>{g}</option>)}
                  </select>
                </div>

                <div className="iim-field full">
                  <label className="iim-field-label">Season</label>
                  <div className="iim-toggle-group">
                    {SEASONS.map(s => {
                      const on = editItemData.season.includes(s);
                      return (
                        <button
                          key={s}
                          className={`iim-toggle${on ? ' on' : ''}`}
                          onClick={() => {
                            const next = on
                              ? editItemData.season.filter(x => x !== s)
                              : [...editItemData.season, s];
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
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" stroke="none"><circle cx="12" cy="12" r="8"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Color</span>
                    <span className="iim-attr-value iim-chip-color">
                      {swatchColor && <span className="iim-color-dot" style={{ background: swatchColor }} />}
                      {selectedItem.color || '—'}
                    </span>
                  </div>
                </div>

                <div className="iim-attr-card">
                  <span className="iim-attr-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
                  </span>
                  <div className="iim-attr-content">
                    <span className="iim-attr-label">Gender</span>
                    <span className="iim-attr-value">{selectedItem.gender || 'Unisex'}</span>
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
            </>
          )}
        </div>
      </div>
    </Modal>
  );
};

export default ItemInspectModal;
