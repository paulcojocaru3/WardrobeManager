import React from 'react';
import Modal from '../Modal';
import Button from '../Button';

const EditOutfitModal = ({ isOpen, onClose, editData, setEditData, clothes, onEditSave, loading }) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Edit Outfit" size="large">
      <div className="edit-outfit-container">
        <input 
          className="name-input" 
          value={editData.name} 
          onChange={e => setEditData({...editData, name: e.target.value})} 
        />
        <div className="edit-items-grid">
          {clothes.map(item => {
            const isSelected = editData.itemIds.includes(item.id);
            return (
              <div key={item.id} className={`selectable-item ${isSelected ? 'selected' : ''}`} onClick={() => {
                if (isSelected) setEditData({...editData, itemIds: editData.itemIds.filter(id => id !== item.id)});
                else {
                  const sameType = clothes.find(c => editData.itemIds.includes(c.id) && c.type === item.type);
                  const newIds = sameType ? [...editData.itemIds.filter(id => id !== sameType.id), item.id] : [...editData.itemIds, item.id];
                  setEditData({...editData, itemIds: newIds});
                }
              }}>
                <img src={item.processedImageUrl} alt="" />
                <div className="check-badge">{isSelected ? '✓' : '+'}</div>
              </div>
            );
          })}
        </div>
        <Button label="Save Outfit" onClick={onEditSave} loading={loading} />
      </div>
    </Modal>
  );
};

export default EditOutfitModal;
