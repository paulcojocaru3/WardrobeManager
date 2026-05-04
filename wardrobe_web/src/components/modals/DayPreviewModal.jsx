import React from 'react';
import Modal from '../Modal';

const DayPreviewModal = ({ 
  isOpen, 
  onClose, 
  previewDay, 
  onWearOutfit, 
  setSelectedPlannerEvent, 
  setSelectedDayIndex, 
  getDayOffset, 
  setView 
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="DAY PREVIEW" size="medium">
      {previewDay && (
        <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
          <div style={{ background: 'var(--bg-subtle)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)', textAlign: 'center' }}>
            <div style={{ fontSize: '1.1rem', fontWeight: 'bold' }}>{previewDay.weekdayLabel}, {previewDay.dayLabel}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--fg-muted)', margin: '5px 0' }}>
              {previewDay.weather?.temperature !== undefined
                ? `${Math.round(previewDay.weather.temperature)}°C • ${previewDay.weather.condition}`
                : 'Forecast pending'}
            </div>
            <div style={{ fontSize: '0.85rem', color: 'var(--accent)', fontWeight: 'bold' }}>
              {previewDay.primaryEvent ? previewDay.primaryEvent.name : 'No planned events'}
            </div>
          </div>

          {previewDay.allEvents && previewDay.allEvents.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', maxHeight: '60vh', overflowY: 'auto' }}>
              {previewDay.allEvents.map((entry, idx) => (
                <div key={idx} style={{ background: 'var(--card-bg)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                  <div style={{ fontSize: '0.9rem', fontWeight: 'bold', marginBottom: '5px', textAlign: 'center', color: 'var(--fg)' }}>
                    {entry.event.name}
                  </div>
                  {entry.itinerary?.outfit ? (
                    <>
                      <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '10px', textAlign: 'center' }}>
                        OUTFIT FOR {entry.itinerary.moment?.toUpperCase() || 'THIS DAY'}
                      </div>
                      <div style={{ display: 'flex', gap: '10px', overflowX: 'auto', paddingBottom: '10px', justifyContent: 'center' }}>
                        {entry.itinerary.outfit.items?.map(item => (
                          <div key={item.id} style={{ width: '80px', height: '80px', borderRadius: '10px', overflow: 'hidden', border: '1px solid var(--border-subtle)', flexShrink: 0 }}>
                            <img src={item.processedImageUrl} alt={item.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                          </div>
                        ))}
                      </div>
                      <div style={{ display: 'flex', gap: '10px', marginTop: '15px' }}>
                        <button 
                          onClick={() => { onWearOutfit(entry.itinerary.outfit.id); onClose(); }} 
                          style={{ flex: 2, background: 'var(--accent-bg)', color: 'var(--accent-fg)', border: '1px solid var(--accent)', padding: '12px', borderRadius: '8px', fontSize: '0.8rem', fontWeight: 'bold', cursor: 'pointer' }}
                        >
                          WEAR IT
                        </button>
                        <button 
                          onClick={() => { 
                            setSelectedPlannerEvent(entry.event);
                            setSelectedDayIndex(getDayOffset(entry.event.startDate, previewDay.date));
                            setView('planner');
                            onClose();
                          }} 
                          style={{ flex: 1, background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border-subtle)', padding: '12px', borderRadius: '8px', fontSize: '0.8rem', fontWeight: 'bold', cursor: 'pointer' }}
                        >
                          PLANNER
                        </button>
                      </div>
                    </>
                  ) : (
                    <>
                      <div style={{ fontSize: '0.8rem', color: 'var(--fg-muted)', marginBottom: '15px', textAlign: 'center' }}>No outfit planned for this event.</div>
                      <button 
                        onClick={() => { 
                          setSelectedPlannerEvent(entry.event);
                          setSelectedDayIndex(getDayOffset(entry.event.startDate, previewDay.date));
                          setView('planner');
                          onClose();
                        }} 
                        style={{ width: '100%', background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border-subtle)', padding: '12px', borderRadius: '8px', fontSize: '0.8rem', fontWeight: 'bold', cursor: 'pointer' }}
                      >
                        + PLAN OUTFIT
                      </button>
                    </>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <div style={{ background: 'var(--card-bg)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border-subtle)', textAlign: 'center' }}>
              <div style={{ fontSize: '0.9rem', color: 'var(--fg-muted)', marginBottom: '15px' }}>No events planned for this day.</div>
            </div>
          )}
        </div>
      )}
    </Modal>
  );
};

export default DayPreviewModal;
