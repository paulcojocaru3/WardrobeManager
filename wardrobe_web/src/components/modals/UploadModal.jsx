import Modal from '../Modal';
import Button from '../Button';

const UploadModal = ({ isOpen, onClose, uploadData, setUploadData, onUpload, loading }) => {
  const isArray = Array.isArray(uploadData);
  const data = isArray ? uploadData : [];

  const updateName = (index, newName) => {
    const newData = [...data];
    newData[index].name = newName;
    setUploadData(newData);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Set Name${data.length > 1 ? 's' : ''}`} size="small">
      <div style={{ maxHeight: '60vh', overflowY: 'auto', marginBottom: '15px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {data.map((item, index) => (
          <div key={index} style={{ display: 'flex', flexDirection: 'column', gap: '5px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--fg-muted)' }}>File: {item.file?.name}</span>
            <input 
              className="name-input" 
              value={item.name} 
              onChange={e => updateName(index, e.target.value)} 
              autoFocus={index === 0}
            />
          </div>
        ))}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <Button label={`Confirm (${data.length} item${data.length > 1 ? 's' : ''})`} onClick={onUpload} loading={loading} />
        <Button label="Cancel" variant="secondary" onClick={onClose} />
      </div>
    </Modal>
  );
};

export default UploadModal;
