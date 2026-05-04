import React from 'react';
import Modal from '../Modal';

const GeneratingModal = ({ isOpen, onClose, generatingProgress }) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="GENERATING OUTFITS" size="small">
      <div style={{ padding: '30px', textAlign: 'center' }}>
        {generatingProgress && (
          <>
            <div style={{ fontSize: '1rem', fontWeight: 'bold', marginBottom: '10px' }}>{generatingProgress.status}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--fg-muted)' }}>
              {generatingProgress.current} / {generatingProgress.total} days processed
            </div>
            <div style={{ marginTop: '15px', background: 'var(--bg-subtle)', borderRadius: '10px', height: '8px', overflow: 'hidden' }}>
              <div style={{ 
                width: `${generatingProgress.total > 0 ? (generatingProgress.current / generatingProgress.total) * 100 : 0}%`, 
                height: '100%', 
                background: 'var(--accent)',
                transition: 'width 0.3s ease'
              }} />
            </div>
          </>
        )}
      </div>
    </Modal>
  );
};

export default GeneratingModal;
