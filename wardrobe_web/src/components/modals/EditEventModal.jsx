import Modal from '../Modal';
import Button from '../Button';
import { USAGES } from '../../constants/wardrobe';

const dayStepper = {
  display: 'inline-flex', alignItems: 'center', gap: '4px',
  border: '1px solid var(--border-subtle)', borderRadius: '10px',
  background: 'var(--bg-soft)', padding: '2px'
};

const dayStepBtn = {
  width: '24px', height: '24px', border: 'none', borderRadius: '8px',
  background: 'transparent', color: 'var(--fg-muted)', cursor: 'pointer',
  fontSize: '1rem', lineHeight: 1, display: 'flex', alignItems: 'center', justifyContent: 'center'
};

const dayStepValue = {
  minWidth: '26px', textAlign: 'center', fontSize: '0.8rem', fontWeight: 600, color: 'var(--fg)'
};

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

        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)' }}>PACK LIGHT MODE</span>
            <button
              onClick={() => {
                if (editEventData.reuseAfterDays) {
                  setEditEventData({...editEventData, reuseAfterDays: null});
                } else {
                  setEditEventData({...editEventData, reuseAfterDays: 3});
                }
              }}
              style={{
                width: '36px', height: '20px', borderRadius: '10px', border: 'none', cursor: 'pointer',
                background: editEventData.reuseAfterDays ? 'var(--accent)' : 'var(--border-muted)',
                position: 'relative', transition: 'background 0.2s'
              }}
            >
              <span style={{
                position: 'absolute', top: '2px',
                left: editEventData.reuseAfterDays ? '18px' : '2px',
                width: '16px', height: '16px', borderRadius: '50%', background: '#fff',
                transition: 'left 0.2s'
              }} />
            </button>
          </div>
          <div style={{ fontSize: '0.65rem', color: 'var(--fg-muted)', marginBottom: '8px' }}>
            Allow reusing tops and bottoms after a cooldown period. Shoes, outerwear and accessories can be reused daily.
          </div>
          {editEventData.reuseAfterDays && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
              <span style={{ fontSize: '0.65rem', color: 'var(--fg-muted)', whiteSpace: 'nowrap' }}>Reuse after</span>
              <div style={dayStepper}>
                <button
                  type="button"
                  style={dayStepBtn}
                  onClick={() => setEditEventData({ ...editEventData, reuseAfterDays: Math.max(2, editEventData.reuseAfterDays - 1) })}
                >
                  −
                </button>
                <span style={dayStepValue}>{editEventData.reuseAfterDays}</span>
                <button
                  type="button"
                  style={dayStepBtn}
                  onClick={() => setEditEventData({ ...editEventData, reuseAfterDays: Math.min(14, editEventData.reuseAfterDays + 1) })}
                >
                  +
                </button>
              </div>
              <span style={{ fontSize: '0.65rem', color: 'var(--fg-muted)' }}>days</span>
            </div>
          )}
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
