import React from 'react';
import Modal from '../Modal';
import Button from '../Button';
import { EVENT_MOMENTS } from '../../constants/wardrobe';

const EditItineraryModal = ({ 
  isOpen, 
  onClose, 
  editItineraryData, 
  setEditItineraryData, 
  outfits, 
  onUpdateItinerary, 
  loading 
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="EDIT ITINERARY" size="medium">
      <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
        {/* Current Outfit Display (read-only) */}
        {editItineraryData.outfitId && (
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '8px' }}>CURRENT OUTFIT</span>
            <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', padding: '10px', background: 'var(--bg-subtle)', borderRadius: '12px' }}>
              {(() => {
                const outfit = outfits.find(o => o.id === editItineraryData.outfitId);
                return outfit ? (
                  <>
                    <div style={{ fontSize: '0.75rem', fontWeight: 'bold', width: '100%', marginBottom: '4px' }}>{outfit.name}</div>
                    {outfit.items?.map(item => (
                      <div key={item.id} style={{ width: '50px', height: '50px', borderRadius: '8px', overflow: 'hidden', border: '1px solid var(--border-subtle)' }}>
                        <img src={item.processedImageUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                      </div>
                    ))}
                  </>
                ) : <span style={{ fontSize: '0.7rem', color: 'var(--fg-muted)' }}>No items</span>;
              })()}
            </div>
            <div style={{ fontSize: '0.6rem', color: 'var(--fg-faint)', marginTop: '8px' }}>To change outfit, please remove this one and plan a new outfit</div>
          </div>
        )}

        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>DATE</span>
          <input
            type="date"
            className="name-input"
            value={editItineraryData.date}
            onChange={(e) => setEditItineraryData({ ...editItineraryData, date: e.target.value })}
          />
        </div>

        <div>
          <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>ACTIVITY / MOMENT</span>
          <select
            className="name-input"
            value={editItineraryData.moment}
            onChange={(e) => setEditItineraryData({ ...editItineraryData, moment: e.target.value })}
            style={{ width: '100%' }}
          >
            <option value="">-- Select Activity --</option>
            {EVENT_MOMENTS.map(moment => (
              <option key={moment} value={moment}>{moment}</option>
            ))}
          </select>
        </div>

        <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
          <Button label="SAVE" onClick={onUpdateItinerary} loading={loading} disabled={!editItineraryData.date || !editItineraryData.moment} />
          <Button label="CANCEL" variant="secondary" onClick={onClose} />
        </div>
      </div>
    </Modal>
  );
};

export default EditItineraryModal;
