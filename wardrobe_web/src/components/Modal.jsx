import './Modal.css';

const Modal = ({ isOpen, onClose, children, title, size = 'medium', contentClassName = '' }) => {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className={`modal-content ${size} ${contentClassName}`.trim()} onClick={e => e.stopPropagation()}>
        {title && <h3 className="modal-title">{title.toUpperCase()}</h3>}
        {children}
      </div>
    </div>
  );
};

export default Modal;
