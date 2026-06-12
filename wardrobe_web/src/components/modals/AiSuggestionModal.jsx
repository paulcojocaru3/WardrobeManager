import React from 'react';
import Modal from '../Modal';
import Button from '../Button';
import { CLOTHING_TYPES } from '../../constants/wardrobe';

// Minimum match score for an alternative to be shown. Tune here (0.5 = acceptable, 0.7 = strong only).
const MIN_ALTERNATIVE_SCORE = 0.7;

// ClothingType serializes as its enum index; map it to a readable slot label.
const slotLabel = (type) =>
  (typeof type === 'number' ? CLOTHING_TYPES[type] : type) || 'ITEM';

const buildIntentChips = (intent) => {
  if (!intent) return [];
  const chips = [];
  if (intent.style) chips.push(intent.style);
  if (intent.occasion) chips.push(intent.occasion);
  if (intent.city) chips.push(intent.city);
  // outfit-level colors (garment-bound colors live on garmentSpecs instead)
  (intent.desiredColors || []).forEach(c => chips.push(c));
  (intent.avoidColors || []).forEach(c => chips.push(`no ${c}`));
  // per-garment colors, e.g. "TOP: no black, no white", "BOTTOM: black"
  (intent.garmentSpecs || []).forEach(g => {
    const parts = [
      ...(g.desiredColors || []),
      ...(g.avoidColors || []).map(c => `no ${c}`),
    ];
    if (parts.length > 0) chips.push(`${slotLabel(g.type)}: ${parts.join(', ')}`);
  });
  if (intent.anchorDescription) chips.push(intent.anchorDescription);
  return chips;
};

// Per type: the currently-selected item for that slot plus its swappable alternatives
// (everything but the current pick, over the threshold, top 3). Derived from selectedItems,
// so after a swap the dropped item reappears as an alternative (swap-back works for free).
const buildSlots = (aiData) =>
  (aiData.recommendationsPerType || [])
    .map(rec => {
      const selected = aiData.selectedItems.find(
        si => (rec.topCandidates || []).some(tc => tc.id === si.id));
      const alternatives = (rec.topCandidates || [])
        .filter(c => !selected || c.id !== selected.id)
        .filter(c => c.similarityScore > MIN_ALTERNATIVE_SCORE)
        .slice(0, 3);
      return { type: rec.type, label: CLOTHING_TYPES[rec.type] ?? 'ITEM', selectedId: selected?.id, alternatives };
    })
    .filter(slot => slot.alternatives.length > 0);

const AiSuggestionModal = ({ isOpen, onClose, aiData, setAiData, intent, onSaveAiOutfit, onRegenerate, loading }) => {
  const intentChips = buildIntentChips(intent);
  const slots = aiData ? buildSlots(aiData) : [];

  // Ids of selected items that occupy a swappable slot (the seed is excluded — it has no slot).
  const slotSelectedIds = new Set(slots.map(s => s.selectedId).filter(Boolean));
  const scoreById = new Map(
    (aiData?.recommendationsPerType || [])
      .flatMap(rec => rec.topCandidates || [])
      .map(c => [c.id, c.similarityScore]));

  // Replace the item currently filling a slot with the chosen alternative.
  const swapItem = (slot, candidate) => {
    if (!slot.selectedId) return;
    setAiData({
      ...aiData,
      selectedItems: aiData.selectedItems.map(si => si.id === slot.selectedId ? candidate : si),
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="AI OUTFIT SUGGESTION" size="large">
      {aiData && (
        <div style={{ maxHeight: '80vh', overflowY: 'auto', padding: '10px' }}>
          <input
            className="name-input"
            value={aiData.name}
            onChange={e => setAiData({...aiData, name: e.target.value})}
            style={{ width: '100%', fontSize: '24px', marginBottom: '20px' }}
          />
          {intentChips.length > 0 && (
            <div style={{ marginBottom: '20px' }}>
              <div style={{ fontSize: '11px', letterSpacing: '1px', opacity: 0.6, marginBottom: '8px' }}>UNDERSTOOD FROM YOUR PROMPT</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                {intentChips.map((chip, i) => (
                  <span key={i} style={{
                    padding: '5px 12px',
                    background: 'var(--card-bg)',
                    border: '1px solid var(--border-subtle)',
                    borderRadius: '999px',
                    fontSize: '12px',
                    fontWeight: 700,
                    textTransform: 'capitalize'
                  }}>{chip}</span>
                ))}
              </div>
            </div>
          )}
          {(aiData.warnings || []).length > 0 && (
            <div style={{
              marginBottom: '20px',
              padding: '12px 14px',
              background: 'rgba(220, 160, 0, 0.10)',
              border: '1px solid rgba(220, 160, 0, 0.45)',
              borderRadius: '10px'
            }}>
              <div style={{ fontSize: '11px', letterSpacing: '1px', opacity: 0.7, marginBottom: '6px' }}>⚠ COULDN'T FULLY MATCH YOUR REQUEST</div>
              <ul style={{ margin: 0, paddingLeft: '18px', fontSize: '13px', lineHeight: 1.5 }}>
                {aiData.warnings.map((w, i) => <li key={i}>{w}</li>)}
              </ul>
            </div>
          )}
          <div className="clothes-grid">
            {aiData.selectedItems.map(item => {
              const score = slotSelectedIds.has(item.id) ? scoreById.get(item.id) : null;
              return (
                <div key={item.id} className="item-card" style={{ position: 'relative' }}>
                  <img src={item.processedImageUrl} alt="" />
                  {score != null && (
                    <span style={{
                      position: 'absolute', top: 6, right: 6,
                      background: 'var(--card-bg)', border: '1px solid var(--border-subtle)',
                      borderRadius: '999px', padding: '2px 8px', fontSize: '11px', fontWeight: 700
                    }}>{Math.round(score * 100)}%</span>
                  )}
                </div>
              );
            })}
          </div>

          {slots.length > 0 && (
            <div style={{ marginTop: '24px' }}>
              <div style={{ fontSize: '11px', letterSpacing: '1px', opacity: 0.6, marginBottom: '12px' }}>TAP AN ALTERNATIVE TO SWAP</div>
              {slots.map(slot => (
                <div key={slot.label} style={{ marginBottom: '16px' }}>
                  <div style={{ fontSize: '12px', fontWeight: 700, marginBottom: '8px' }}>{slot.label}</div>
                  <div className="clothes-grid">
                    {slot.alternatives.map(c => (
                      <div
                        key={c.id}
                        className="item-card"
                        style={{ position: 'relative', cursor: 'pointer' }}
                        onClick={() => swapItem(slot, c)}
                        title="Tap to swap into the outfit"
                      >
                        <img src={c.processedImageUrl} alt="" />
                        <span style={{
                          position: 'absolute', top: 6, right: 6,
                          background: 'var(--card-bg)', border: '1px solid var(--border-subtle)',
                          borderRadius: '999px', padding: '2px 8px', fontSize: '11px', fontWeight: 700
                        }}>{Math.round(c.similarityScore * 100)}%</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="modal-actions" style={{ marginTop: '20px' }}>
            <Button label="CONFIRM & SAVE" onClick={onSaveAiOutfit} loading={loading} />
            {onRegenerate && (
              <Button label="GENERATE ANOTHER" variant="secondary" onClick={onRegenerate} loading={loading} />
            )}
            <Button label="DISCARD" variant="secondary" onClick={onClose} />
          </div>
        </div>
      )}
    </Modal>
  );
};

export default AiSuggestionModal;
