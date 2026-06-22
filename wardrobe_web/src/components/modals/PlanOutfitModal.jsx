import Modal from '../Modal';
import Button from '../Button';
import { EVENT_MOMENTS } from '../../constants/wardrobe';

const PlanOutfitModal = ({ 
  isOpen, 
  onClose, 
  planData, 
  setPlanData, 
  plannerEvents, 
  currentEventDays, 
  onPlanOutfit, 
  loading 
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="PLAN OUTFIT TO EVENT" size="medium">
      <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
        {planData.outfitId === null && planData.plannerEventId && planData.selectedDayIndex !== null ? (
          <div style={{ background: 'var(--bg-subtle)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>EVENT</div>
            <div style={{ fontWeight: 'bold', marginBottom: '10px' }}>
              {plannerEvents.find(e => e.id === planData.plannerEventId)?.name}
            </div>
            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>DAY</div>
            <div style={{ fontWeight: 'bold' }}>
              {currentEventDays.find(d => d.index === planData.selectedDayIndex)?.label}
            </div>
          </div>
        ) : (
          <>
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>SELECT EVENT</span>
              <select 
                className="name-input" 
                value={planData.plannerEventId} 
                onChange={e => setPlanData({...planData, plannerEventId: e.target.value, selectedDayIndex: null})}
                style={{ width: '100%' }}
              >
                <option value="">-- Select Event --</option>
                {plannerEvents.map(event => (
                  <option key={event.id} value={event.id}>
                    {event.name} ({new Date(event.startDate).toLocaleDateString()} - {new Date(event.endDate).toLocaleDateString()})
                  </option>
                ))}
              </select>
            </div>
            
            {planData.plannerEventId && (
              <div>
                <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>SELECT DAY</span>
                <select 
                  className="name-input" 
                  value={planData.selectedDayIndex !== null ? planData.selectedDayIndex : ''}
                  onChange={e => setPlanData({...planData, selectedDayIndex: parseInt(e.target.value)})}
                  style={{ width: '100%' }}
                >
                  <option value="">-- Select Day --</option>
                  {currentEventDays.map(day => (
                    <option key={day.index} value={day.index}>
                      {day.label}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </>
        )}
        
        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>ACTIVITY / MOMENT</span>
          <select 
            className="name-input" 
            value={planData.moment} 
            onChange={e => setPlanData({...planData, moment: e.target.value})}
            style={{ width: '100%' }}
          >
            <option value="">-- Select Activity --</option>
            {EVENT_MOMENTS.map(moment => (
              <option key={moment} value={moment}>{moment}</option>
            ))}
          </select>
        </div>
        
        <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
          <Button label="PLAN" onClick={onPlanOutfit} loading={loading} disabled={!planData.plannerEventId || planData.selectedDayIndex === null || !planData.moment} />
          <Button label="CANCEL" variant="secondary" onClick={onClose} />
        </div>
      </div>
    </Modal>
  );
};

export default PlanOutfitModal;
