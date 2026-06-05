import React from 'react';
import Modal from '../Modal';
import Button from '../Button';

const buildIntentChips = (intent) => {
  if (!intent) return [];
  const chips = [];
  if (intent.style) chips.push(intent.style);
  if (intent.occasion) chips.push(intent.occasion);
  if (intent.city) chips.push(intent.city);
  (intent.desiredColors || []).forEach(c => chips.push(c));
  (intent.avoidColors || []).forEach(c => chips.push(`no ${c}`));
  if (intent.anchorDescription) chips.push(intent.anchorDescription);
  return chips;
};

const AiSuggestionModal = ({ isOpen, onClose, aiData, setAiData, intent, onSaveAiOutfit, loading }) => {
  const intentChips = buildIntentChips(intent);
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
          <div className="clothes-grid">
            {aiData.selectedItems.map(item => (
              <div key={item.id} className="item-card">
                <img src={item.processedImageUrl} alt="" />
              </div>
            ))}
          </div>
          <div className="modal-actions" style={{ marginTop: '20px' }}>
            <Button label="CONFIRM & SAVE" onClick={onSaveAiOutfit} loading={loading} />
            <Button label="DISCARD" variant="secondary" onClick={onClose} />
          </div>
        </div>
      )}
    </Modal>
  );
};

export default AiSuggestionModal;
