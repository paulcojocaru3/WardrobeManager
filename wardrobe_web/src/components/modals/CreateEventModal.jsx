import React from 'react';
import Modal from '../Modal';
import Button from '../Button';
import { USAGES } from '../../constants/wardrobe';

const CreateEventModal = ({ 
  isOpen, 
  onClose, 
  wizardStep, 
  setWizardStep, 
  wizardPreview, 
  setWizardPreview, 
  createEventData, 
  setCreateEventData, 
  eventLocationSearch, 
  setEventLocationSearch, 
  eventLocationSuggestions, 
  setEventLocationSuggestions, 
  onPreviewEvent, 
  onCreatePlannerEvent, 
  wizardLoading, 
  loading 
}) => {
  return (
    <Modal 
      isOpen={isOpen} 
      onClose={onClose} 
      title={wizardStep === 0 ? "CREATE NEW EVENT" : "EVENT PREVIEW"} 
      size="large"
    >
      {/* Step Indicator */}
      <div style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '20px', padding: '0 10px' }}>
        {[0, 1].map(step => (
          <div key={step} style={{ 
            width: '80px', 
            height: '4px', 
            borderRadius: '2px', 
            background: step <= wizardStep ? 'var(--accent)' : 'var(--border-subtle)',
            transition: 'all 0.3s'
          }} />
        ))}
      </div>

      {/* Step 0: Event Details */}
      {wizardStep === 0 && (
        <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT NAME</span>
            <input 
              type="text" 
              className="name-input" 
              value={createEventData.name} 
              onChange={e => setCreateEventData({...createEventData, name: e.target.value})}
              placeholder="e.g. Summer Vacation 2026"
              autoFocus
            />
          </div>
          
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT TYPE</span>
            <select 
              className="name-input" 
              value={createEventData.type} 
              onChange={e => setCreateEventData({...createEventData, type: e.target.value})}
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
            <div style={{ position: 'relative' }}>
              <input 
                type="text" 
                className="name-input" 
                value={createEventData.location} 
                onChange={e => {
                  setCreateEventData({...createEventData, location: e.target.value});
                  setEventLocationSearch(e.target.value);
                }}
                placeholder="e.g. Paris, France"
              />
              {eventLocationSuggestions.length > 0 && (
                <div style={{ 
                  position: 'absolute', 
                  top: '100%', 
                  left: 0, 
                  right: 0, 
                  background: 'var(--card-bg)', 
                  border: '1px solid var(--border-subtle)', 
                  borderRadius: '8px',
                  maxHeight: '150px',
                  overflowY: 'auto',
                  zIndex: 1000,
                  marginTop: '4px'
                }}>
                  {eventLocationSuggestions.map((city, idx) => (
                    <button 
                      key={idx}
                      onClick={() => {
                        setCreateEventData({...createEventData, location: `${city.name}, ${city.country}`});
                        setEventLocationSearch('');
                        setEventLocationSuggestions([]);
                      }}
                      style={{ 
                        width: '100%', 
                        padding: '8px 12px', 
                        textAlign: 'left', 
                        background: 'none', 
                        border: 'none',
                        borderBottom: '1px solid var(--border-subtle)',
                        color: 'var(--fg)',
                        cursor: 'pointer',
                        fontSize: '0.8rem'
                      }}
                    >
                      {city.name}, {city.country}
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>
          
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>START DATE</span>
              <input 
                type="date" 
                className="name-input" 
                value={createEventData.startDate} 
                onChange={e => {
                  const newStartDate = e.target.value;
                  setCreateEventData({
                    ...createEventData, 
                    startDate: newStartDate,
                    endDate: createEventData.endDate || newStartDate
                  });
                }}
              />
            </div>
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>END DATE</span>
              <input 
                type="date" 
                className="name-input" 
                value={createEventData.endDate} 
                onChange={e => setCreateEventData({...createEventData, endDate: e.target.value})}
              />
            </div>
          </div>

          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>TRIP VIBE / STYLE PERSONA (Optional)</span>
            <div style={{ fontSize: '0.65rem', color: 'var(--fg-muted)', marginBottom: '8px' }}>
              Leave empty to let AI decide based on the event type, or force a specific style for this trip.
            </div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
              {USAGES.map(style => {
                const isSelected = createEventData.preferredStyles?.includes(style);
                return (
                  <button
                    key={style}
                    onClick={() => {
                      const newStyles = isSelected 
                        ? createEventData.preferredStyles.filter(s => s !== style)
                        : [...(createEventData.preferredStyles || []), style];
                      setCreateEventData({...createEventData, preferredStyles: newStyles});
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
            <Button 
              label="NEXT PREVIEW" 
              onClick={onPreviewEvent} 
              loading={wizardLoading} 
              disabled={!createEventData.name || !createEventData.location || !createEventData.startDate || !createEventData.endDate} 
            />
            <Button 
              label="CANCEL" 
              variant="secondary" 
              onClick={onClose} 
            />
          </div>
        </div>
      )}

      {/* Step 1: Preview with Weather */}
      {wizardStep === 1 && wizardPreview && (
        <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
          <div style={{ 
            background: 'var(--bg-subtle)', 
            padding: '15px', 
            borderRadius: '12px', 
            border: '1px solid var(--border-subtle)',
            marginBottom: '10px'
          }}>
            <div style={{ fontWeight: 'bold', fontSize: '0.9rem', marginBottom: '5px' }}>{createEventData.name}</div>
            <div style={{ fontSize: '0.75rem', color: 'var(--fg-muted)' }}>
              {createEventData.type} • {createEventData.location}
            </div>
            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginTop: '5px' }}>
              {new Date(createEventData.startDate).toLocaleDateString()} - {new Date(createEventData.endDate).toLocaleDateString()}
            </div>
          </div>

          <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>
            WEATHER FORECAST ({wizardPreview.location})
          </div>
          
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))', 
            gap: '10px',
            maxHeight: '250px',
            overflowY: 'auto'
          }}>
            {wizardPreview.days.map((day, idx) => (
              <div key={idx} style={{ 
                background: 'var(--card-bg)', 
                padding: '10px', 
                borderRadius: '10px', 
                border: '1px solid var(--border-subtle)',
                textAlign: 'center'
              }}>
                <div style={{ fontSize: '0.6rem', color: 'var(--fg-muted)', marginBottom: '4px' }}>
                  {day.date.toLocaleDateString(undefined, { weekday: 'short' })}
                </div>
                <div style={{ fontSize: '0.75rem', fontWeight: 'bold' }}>
                  Day {day.dayNumber}
                </div>
                <div style={{ 
                  background: 'var(--accent-bg)', 
                  padding: '4px 8px', 
                  borderRadius: '8px',
                  marginTop: '6px'
                }}>
                  <div style={{ fontSize: '0.8rem', fontWeight: 'bold', color: 'var(--accent-fg)' }}>
                    {Math.round(day.weather?.temperature || 20)}°C
                  </div>
                  <div style={{ fontSize: '0.5rem', color: 'var(--accent-fg)', opacity: 0.8 }}>
                    {day.weather?.condition || 'N/A'}
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
            <Button label="BACK" variant="secondary" onClick={() => setWizardStep(0)} />
            <Button label="CREATE EVENT" onClick={onCreatePlannerEvent} loading={loading} />
          </div>
        </div>
      )}
    </Modal>
  );
};

export default CreateEventModal;
