import React from 'react';
import Modal from '../Modal';
import Button from '../Button';
import { USAGES } from '../../constants/wardrobe';

const EditEventModal = ({ 
  isOpen, 
  onClose, 
  editEventData, 
  setEditEventData, 
  onUpdatePlannerEvent, 
  loading 
}) => {
  return (
    <Modal 
      isOpen={isOpen} 
      onClose={onClose} 
      title="EDIT EVENT DETAILS" 
      size="medium"
    >
      <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT NAME</span>
          <input 
            type="text" 
            className="name-input" 
            value={editEventData.name} 
            onChange={e => setEditEventData({...editEventData, name: e.target.value})}
          />
        </div>
        
        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT TYPE</span>
          <select 
            className="name-input" 
            value={editEventData.type} 
            onChange={e => setEditEventData({...editEventData, type: e.target.value})}
            style={{ width: '100%' }}
          >
            <option value="Vacation">Vacation</option>
            <option value="Business Trip">Business Trip</option>
            <option value="Wedding">Wedding</option>
            <option value="Party">Party</option>
            <option value="Meeting">Meeting</option>
            <option value="Date">Date</option>
            <option value="Weekend">Weekend</option>
            <option value="Other">Other</option>
          </select>
        </div>
        
        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>LOCATION</span>
          <input 
            type="text" 
            className="name-input" 
            value={editEventData.location} 
            onChange={e => setEditEventData({...editEventData, location: e.target.value})}
          />
        </div>
        
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>START DATE</span>
            <input 
              type="date" 
              className="name-input" 
              value={editEventData.startDate} 
              onChange={e => setEditEventData({...editEventData, startDate: e.target.value})}
            />
          </div>
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>END DATE</span>
            <input 
              type="date" 
              className="name-input" 
              value={editEventData.endDate} 
              onChange={e => setEditEventData({...editEventData, endDate: e.target.value})}
            />
          </div>
        </div>

        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>TRIP VIBE / STYLE PERSONA</span>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {USAGES.map(style => {
              const isSelected = editEventData.preferredStyles?.includes(style);
              return (
                <button
                  key={style}
                  onClick={() => {
                    const newStyles = isSelected 
                      ? editEventData.preferredStyles.filter(s => s !== style)
                      : [...(editEventData.preferredStyles || []), style];
                    setEditEventData({...editEventData, preferredStyles: newStyles});
                  }}
                  style={{
                    padding: '6px 12px', fontSize: '0.7rem', borderRadius: '20px', 
                    border: isSelected ? '1px solid var(--accent)' : '1px solid var(--border-muted)',
                    background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', 
                    color: isSelected ? 'var(--accent-fg)' : 'var(--fg-muted)', 
                    cursor: 'pointer', transition: 'all 0.2s'
                  }}
                >
                  {style}
                </button>
              );
            })}
          </div>
        </div>
        
        <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
          <Button label="SAVE CHANGES" onClick={onUpdatePlannerEvent} loading={loading} disabled={!editEventData.name || !editEventData.location || !editEventData.startDate || !editEventData.endDate} />
          <Button label="CANCEL" variant="secondary" onClick={onClose} />
        </div>
      </div>
    </Modal>
  );
};

export default EditEventModal;
