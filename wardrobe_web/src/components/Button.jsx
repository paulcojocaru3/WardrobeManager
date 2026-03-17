import React from 'react';
import './Button.css';

const Button = ({ label, onClick, variant = 'primary', loading = false, disabled = false, type = 'button' }) => {
  return (
    <button 
      type={type}
      className={`custom-btn ${variant} ${loading ? 'loading' : ''}`} 
      onClick={onClick} 
      disabled={disabled || loading}
    >
      {loading ? '...' : label.toUpperCase()}
    </button>
  );
};

export default Button;
