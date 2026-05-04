import React from 'react';
import Modal from '../Modal';
import Button from '../Button';

const AiSuggestionModal = ({ isOpen, onClose, aiData, setAiData, onSaveAiOutfit, loading }) => {
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
