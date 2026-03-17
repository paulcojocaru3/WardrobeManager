import React from 'react';
import './Modal.css';

const Modal = ({ isOpen, onClose, children, title, size = 'medium' }) => {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className={`modal-content ${size}`} onClick={e => e.stopPropagation()}>
        {title && <h3 className="modal-title">{title.toUpperCase()}</h3>}
        {children}
      </div>
    </div>
  );
};

export default Modal;
