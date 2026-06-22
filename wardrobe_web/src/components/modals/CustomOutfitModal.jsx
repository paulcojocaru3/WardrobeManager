import Modal from '../Modal';
import Button from '../Button';
import { CLOTHING_TYPES } from '../../constants/wardrobe';

const CustomOutfitModal = ({ 
  isOpen, 
  onClose, 
  customOutfitData, 
  setCustomOutfitData, 
  customOutfitTab, 
  setCustomOutfitTab, 
  clothes, 
  onSaveCustomOutfit, 
  loading 
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="BUILD CUSTOM OUTFIT" size="large">
      <div className="edit-outfit-container">
        <input 
          className="name-input" 
          placeholder="Name your outfit (e.g. Casual Friday)..." 
          value={customOutfitData.name} 
          onChange={e => setCustomOutfitData({...customOutfitData, name: e.target.value})} 
        />

        <div style={{ marginBottom: '20px' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 'bold', color: 'var(--fg-muted)', marginBottom: '8px', textTransform: 'uppercase' }}>Tags</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {['work', 'comfy', 'going-out', 'casual', 'formal', 'gym'].map(tag => {
              const isSelected = (customOutfitData.tags || []).includes(tag);
              return (
                <button
                  key={tag}
                  onClick={() => {
                    const currentTags = customOutfitData.tags || [];
                    if (isSelected) {
                      setCustomOutfitData({ ...customOutfitData, tags: currentTags.filter(t => t !== tag) });
                    } else {
                      setCustomOutfitData({ ...customOutfitData, tags: [...currentTags, tag] });
                    }
                  }}
                  style={{
                    padding: '6px 12px',
                    borderRadius: '16px',
                    border: `1px solid ${isSelected ? 'var(--accent)' : 'var(--border)'}`,
                    background: isSelected ? 'var(--accent)' : 'var(--bg-raised)',
                    color: isSelected ? 'var(--accent-fg)' : 'var(--fg)',
                    fontSize: '0.7rem',
                    fontWeight: 'bold',
                    cursor: 'pointer',
                    transition: 'all 0.2s'
                  }}
                >
                  {isSelected ? `✓ ${tag}` : `+ ${tag}`}
                </button>
              );
            })}
          </div>
        </div>
        
        <div style={{ display: 'flex', gap: '10px', overflowX: 'auto', paddingBottom: '10px', marginBottom: '20px' }}>
          {CLOTHING_TYPES.map((type, idx) => (
            <button 
              key={type}
              onClick={() => setCustomOutfitTab(idx)}
              style={{ 
                padding: '8px 16px', 
                borderRadius: '20px', 
                border: 'none', 
                background: customOutfitTab === idx ? 'var(--accent-bg)' : 'var(--bg-raised)', 
                color: customOutfitTab === idx ? 'var(--accent-fg)' : 'var(--fg-muted)',
                fontSize: '0.7rem',
                fontWeight: 'bold',
                cursor: 'pointer',
                transition: 'all 0.2s'
              }}
            >
              {type}
            </button>
          ))}
        </div>

        <div className="edit-items-grid" style={{ maxHeight: '400px', overflowY: 'auto', padding: '10px', border: '1px solid var(--border-subtle)', borderRadius: '15px' }}>
          {clothes.filter(c => c.type === customOutfitTab).map(item => {
            const isSelected = customOutfitData.itemIds.includes(item.id);
            return (
              <div key={item.id} className={`selectable-item ${isSelected ? 'selected' : ''}`} onClick={() => {
                if (isSelected) {
                  setCustomOutfitData({...customOutfitData, itemIds: customOutfitData.itemIds.filter(id => id !== item.id)});
                } else {
                  const sameType = clothes.find(c => customOutfitData.itemIds.includes(c.id) && c.type === item.type);
                  const newIds = sameType ? [...customOutfitData.itemIds.filter(id => id !== sameType.id), item.id] : [...customOutfitData.itemIds, item.id];
                  setCustomOutfitData({...customOutfitData, itemIds: newIds});
                }
              }}>
                <img src={item.processedImageUrl} alt="" />
                <div className="check-badge">{isSelected ? '✓' : '+'}</div>
              </div>
            );
          })}
          {clothes.filter(c => c.type === customOutfitTab).length === 0 && (
            <div style={{ gridColumn: '1/-1', textAlign: 'center', padding: '40px', color: 'var(--fg-faint)', fontSize: '0.8rem' }}>
              No items in this category.
            </div>
          )}
        </div>
        
        <div style={{ marginTop: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ display: 'flex', gap: '5px' }}>
            {customOutfitData.itemIds.map(id => {
              const item = clothes.find(c => c.id === id);
              if (!item) return null;
              return (
                <div key={id} style={{ width: '30px', height: '30px', borderRadius: '50%', overflow: 'hidden', border: '1px solid var(--border-subtle)' }}>
                  <img src={item.processedImageUrl} style={{ width: '100%', height: '100%', objectFit: 'cover' }} alt=""/>
                </div>
              )
            })}
          </div>
          <Button label="SAVE OUTFIT" onClick={onSaveCustomOutfit} loading={loading} />
        </div>
      </div>
    </Modal>
  );
};

export default CustomOutfitModal;
