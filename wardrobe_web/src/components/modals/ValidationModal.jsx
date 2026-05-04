import React from 'react';
import Modal from '../Modal';

const ValidationModal = ({ isOpen, onClose, renderValidationStep }) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Verify AI Prediction" size="medium">
      {renderValidationStep()}
    </Modal>
  );
};

export default ValidationModal;
