import React from 'react';
import Modal from '../Modal';
import { USAGES } from '../../constants/wardrobe';

const StyleSelectionModal = ({ isOpen, onClose, executeGeneration }) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="SELECT OUTFIT STYLE" size="medium">
      <div style={{ padding: '10px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '15px' }}>
          {USAGES.map(style => (
            <button 
              key={style} 
              onClick={() => executeGeneration(style)} 
              style={{ 
                padding: '20px', 
                background: 'var(--card-bg)', 
                color: 'var(--fg)', 
                border: '1px solid var(--border-subtle)', 
                borderRadius: '15px', 
                cursor: 'pointer', 
                display: 'flex', 
                flexDirection: 'column', 
                alignItems: 'center', 
                gap: '10px' 
              }}
            >
              <span style={{ fontWeight: '900', fontSize: '0.9rem', letterSpacing: '1px' }}>{style.toUpperCase()}</span>
            </button>
          ))}
        </div>
      </div>
    </Modal>
  );
};

export default StyleSelectionModal;
