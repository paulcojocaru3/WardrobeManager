import React from 'react';
import Modal from '../Modal';
import { CLOTHING_TYPES, COLORS, GENDERS, SEASONS, USAGES } from '../../constants/wardrobe';
import { toStringArray } from '../../utils/wardrobeTransforms';

const ItemInspectModal = ({ 
  isOpen, 
  onClose, 
  selectedItem, 
  editItemMode, 
  setEditItemMode, 
  editItemData, 
  setEditItemData, 
  onUpdateItem, 
  onGenerate, 
  loading 
}) => {
  return (
    <Modal 
      isOpen={isOpen} 
      onClose={onClose} 
      title={editItemMode ? `Editing ${selectedItem?.name}` : selectedItem?.name} 
      size="medium"
    >
      {selectedItem && (
        <div style={{ maxHeight: '80vh', overflowY: 'auto', padding: '10px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div style={{ textAlign: 'center', background: 'var(--bg-subtle)', borderRadius: '20px', padding: '15px', border: '1px solid var(--border-subtle)' }}>
            <img src={selectedItem.processedImageUrl} alt="" style={{ maxWidth: '100%', maxHeight: '350px', borderRadius: '15px', objectFit: 'contain' }} />
          </div>

          {editItemMode ? (
            <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
              <div style={{ gridColumn: 'span 2' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>NAME</span>
                <input className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.name} onChange={e => setEditItemData({...editItemData, name: e.target.value})} />
              </div>
              <div>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>TYPE</span>
                <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={typeof editItemData.type === 'number' ? CLOTHING_TYPES[editItemData.type] : editItemData.type} onChange={e => setEditItemData({...editItemData, type: CLOTHING_TYPES.indexOf(e.target.value)})}>
                  {CLOTHING_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>COLOR</span>
                <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.color} onChange={e => setEditItemData({...editItemData, color: e.target.value})}>
                  {COLORS.map(c => <option key={c} value={c}>{c}</option>)}
                </select>
              </div>
              <div>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>GENDER</span>
                <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.gender} onChange={e => setEditItemData({...editItemData, gender: e.target.value})}>
                  {GENDERS.map(g => <option key={g} value={g}>{g}</option>)}
                </select>
              </div>
              <div style={{ gridColumn: 'span 2' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '10px' }}>SEASON (MULTI-SELECT)</span>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                  {SEASONS.map(s => {
                    const isSelected = editItemData.season.includes(s);
                    return (
                      <button 
                        key={s} 
                        onClick={() => {
                          const newSeasons = isSelected ? editItemData.season.filter(item => item !== s) : [...editItemData.season, s];
                          setEditItemData({...editItemData, season: newSeasons});
                        }}
                        style={{
                          padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid var(--accent)' : '1px solid var(--border-muted)',
                          background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', color: isSelected ? 'var(--accent-fg)' : 'var(--fg-muted)', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                        }}
                      >{s.toUpperCase()}</button>
                    );
                  })}
                </div>
              </div>
              <div style={{ gridColumn: 'span 2' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '10px' }}>USAGE / STYLE (MULTI-SELECT)</span>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                  {USAGES.map(u => {
                    const isSelected = editItemData.usage.includes(u);
                    return (
                      <button 
                        key={u} 
                        onClick={() => {
                          const newUsage = isSelected ? editItemData.usage.filter(item => item !== u) : [...editItemData.usage, u];
                          setEditItemData({...editItemData, usage: newUsage});
                        }}
                        style={{
                          padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid var(--accent)' : '1px solid var(--border-muted)',
                          background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', color: isSelected ? 'var(--accent-fg)' : 'var(--fg-muted)', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                        }}
                      >{u.toUpperCase()}</button>
                    );
                  })}
                </div>
              </div>
            </div>
          ) : (
            <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
              <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>TYPE</span>
                <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{CLOTHING_TYPES[selectedItem.type] || selectedItem.type}</span>
              </div>
              <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>COLOR</span>
                <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.color?.toUpperCase()}</span>
              </div>
              <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>GENDER</span>
                <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.gender?.toUpperCase() || 'UNISEX'}</span>
              </div>
              <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>SEASON</span>
                <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.season?.toUpperCase() || 'ANY'}</span>
              </div>
              <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)', gridColumn: 'span 2' }}>
                <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>USAGE</span>
                <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.usage?.toUpperCase() || 'CASUAL'}</span>
              </div>
            </div>
          )}

          <div className="modal-actions" style={{ display: 'flex', gap: '10px' }}>
            {editItemMode ? (
              <>
                <button className="gen-btn" onClick={onUpdateItem} disabled={loading} style={{ flex: 2 }}>
                  {loading ? 'SAVING...' : 'SAVE CHANGES'}
                </button>
                <button className="close-link" onClick={() => setEditItemMode(false)} style={{ flex: 1 }}>
                  CANCEL
                </button>
              </>
            ) : (
              <>
                <button className="gen-btn" onClick={() => onGenerate(selectedItem)} disabled={loading} style={{ flex: 2 }}>
                  {loading ? 'GENERATING...' : 'GENERATE OUTFIT'}
                </button>
                <button className="close-link" onClick={() => { 
                  setEditItemData({
                    ...selectedItem, 
                    season: toStringArray(selectedItem.season),
                    usage: toStringArray(selectedItem.usage)
                  }); 
                  setEditItemMode(true); 
                }} style={{ flex: 1 }}>
                  EDIT
                </button>
              </>
            )}
          </div>
        </div>
      )}
    </Modal>
  );
};

export default ItemInspectModal;
