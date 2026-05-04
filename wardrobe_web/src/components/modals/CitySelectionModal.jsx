import React from 'react';
import Modal from '../Modal';

const CitySelectionModal = ({ 
  isOpen, 
  onClose, 
  searchTerm, 
  setSearchTerm, 
  citySuggestions, 
  handleCityChange 
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="SELECT LOCATION" size="small">
      <div style={{ padding: '10px' }}>
        <input 
          className="name-input" 
          placeholder="Type city..." 
          value={searchTerm} 
          onChange={(e) => setSearchTerm(e.target.value)} 
          autoFocus 
        />
        <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
          {citySuggestions.map((c, idx) => (
            <button 
              key={idx} 
              onClick={() => { handleCityChange(c.name); onClose(); }} 
              style={{ 
                width: '100%', 
                padding: '10px', 
                textAlign: 'left', 
                background: 'none', 
                border: '1px solid var(--border-subtle)', 
                marginBottom: '5px', 
                color: 'var(--fg)' 
              }}
            >
              {c.name}, {c.country}
            </button>
          ))}
        </div>
      </div>
    </Modal>
  );
};

export default CitySelectionModal;
