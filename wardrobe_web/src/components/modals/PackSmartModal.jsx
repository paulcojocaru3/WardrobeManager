import Modal from '../Modal';

const PackSmartModal = ({ isOpen, onClose, packSmartData, packedItems, setPackedItems }) => {
  if (!packSmartData) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="PACK SMART: LUGGAGE OPTIMIZER" size="large">
      <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '20px', maxHeight: '70vh', overflowY: 'auto' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
          <div style={{ background: 'var(--bg-subtle)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>EVENT</div>
            <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{packSmartData.event.name}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--fg-muted)', marginTop: '5px' }}>
              {new Date(packSmartData.event.startDate).toLocaleDateString()} - {new Date(packSmartData.event.endDate).toLocaleDateString()}
            </div>
          </div>
          <div style={{ background: 'var(--accent-bg)', padding: '15px', borderRadius: '12px', border: '1px solid var(--accent)', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
            <div style={{ fontSize: '0.7rem', color: 'var(--accent-fg)', opacity: 0.8, marginBottom: '5px', textTransform: 'uppercase', letterSpacing: '1px' }}>Estimated Luggage</div>
            <div style={{ fontWeight: 'bold', fontSize: '1.2rem', color: 'var(--accent-fg)', textAlign: 'center' }}>{packSmartData.luggageEstimate}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--accent-fg)', marginTop: '5px' }}>
              {packSmartData.totalUnique} unique items
            </div>
          </div>
        </div>

        {packSmartData.inefficiencies.length > 0 && (
          <div style={{ background: 'var(--card-bg)', padding: '15px', borderRadius: '12px', border: '1px dashed var(--accent)', position: 'relative' }}>
            <div style={{ fontSize: '0.8rem', fontWeight: 'bold', color: 'var(--accent)', marginBottom: '10px', display: 'flex', alignItems: 'center', gap: '5px' }}>
              <span style={{ fontSize: '1.2rem' }}>💡</span> Reusability Insights
            </div>
            <ul style={{ margin: 0, paddingLeft: '20px', color: 'var(--fg-muted)', fontSize: '0.8rem', display: 'flex', flexDirection: 'column', gap: '5px' }}>
              {packSmartData.inefficiencies.map((msg, i) => (
                <li key={i}>{msg}</li>
              ))}
            </ul>
          </div>
        )}

        <div>
          <div style={{ fontSize: '0.9rem', fontWeight: 'bold', marginBottom: '15px', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '10px' }}>
            Packing Checklist
          </div>
          
          {packSmartData.totalUnique === 0 ? (
            <div style={{ textAlign: 'center', padding: '30px', color: 'var(--fg-muted)', fontSize: '0.9rem' }}>
              No outfits planned yet for this event.
            </div>
          ) : (
            Object.keys(packSmartData.groupedByType).map(type => (
              <div key={type} style={{ marginBottom: '20px' }}>
                <div style={{ fontSize: '0.75rem', fontWeight: 'bold', color: 'var(--fg-faint)', textTransform: 'uppercase', letterSpacing: '1px', marginBottom: '10px' }}>
                  {type} ({packSmartData.groupedByType[type].length})
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: '10px' }}>
                  {packSmartData.groupedByType[type].map(item => {
                    const isPacked = packedItems.includes(item.id);
                    return (
                      <div 
                        key={item.id} 
                        onClick={() => {
                          if (isPacked) setPackedItems(packedItems.filter(id => id !== item.id));
                          else setPackedItems([...packedItems, item.id]);
                        }}
                        style={{ 
                          display: 'flex', alignItems: 'center', gap: '10px', 
                          background: isPacked ? 'var(--bg-subtle)' : 'var(--card-bg)', 
                          padding: '10px', borderRadius: '10px', border: '1px solid', 
                          borderColor: isPacked ? 'var(--accent)' : 'var(--border-subtle)',
                          cursor: 'pointer', transition: 'all 0.2s', opacity: isPacked ? 0.7 : 1
                        }}
                      >
                        <div style={{ 
                          width: '20px', height: '20px', borderRadius: '4px', 
                          border: '2px solid', borderColor: isPacked ? 'var(--accent)' : 'var(--border-muted)',
                          background: isPacked ? 'var(--accent)' : 'transparent',
                          display: 'flex', alignItems: 'center', justifyContent: 'center',
                          color: '#fff', fontSize: '12px', fontWeight: 'bold'
                        }}>
                          {isPacked ? '✓' : ''}
                        </div>
                        <div style={{ width: '40px', height: '40px', borderRadius: '6px', overflow: 'hidden', flexShrink: 0 }}>
                          <img src={item.processedImageUrl} alt={item.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        </div>
                        <div style={{ flex: 1, overflow: 'hidden' }}>
                          <div style={{ fontSize: '0.8rem', fontWeight: 'bold', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', textDecoration: isPacked ? 'line-through' : 'none' }}>{item.name}</div>
                          <div style={{ fontSize: '0.65rem', color: 'var(--fg-muted)' }}>Worn {item.count} time{item.count !== 1 ? 's' : ''}</div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </Modal>
  );
};

export default PackSmartModal;
