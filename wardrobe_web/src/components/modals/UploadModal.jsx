import React from 'react';
import Modal from '../Modal';
import Button from '../Button';

const UploadModal = ({ isOpen, onClose, uploadData, setUploadData, onUpload, loading }) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Set Name" size="small">
      <input 
        className="name-input" 
        value={uploadData.name} 
        onChange={e => setUploadData({...uploadData, name: e.target.value})} 
        autoFocus 
      />
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <Button label="Confirm" onClick={onUpload} loading={loading} />
        <Button label="Cancel" variant="secondary" onClick={onClose} />
      </div>
    </Modal>
  );
};

export default UploadModal;
