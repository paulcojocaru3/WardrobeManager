import Modal from '../Modal';

const GeneratingModal = ({ isOpen, onClose, generatingProgress }) => {
  const stylistMode = generatingProgress?.mode === 'stylist';

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={stylistMode ? 'GEMMA3 STYLIST' : 'GENERATING OUTFITS'} size="small">
      <div style={{ padding: '30px', textAlign: stylistMode ? 'left' : 'center' }}>
        {stylistMode ? (
          <div>
            <div style={{ fontSize: '1rem', fontWeight: 800, marginBottom: '8px' }}>{generatingProgress.status}</div>
            <div style={{ fontSize: '0.82rem', color: 'var(--fg-muted)', lineHeight: 1.5, marginBottom: '18px' }}>
              {generatingProgress.detail}
            </div>
            <div style={{ display: 'grid', gap: '10px' }}>
              {[
                'Casting wardrobe candidates with FashionCLIP',
                'Checking weather, formality and slot coverage',
                'Waiting for Gemma3 to compose the final look'
              ].map((step, index) => (
                <div key={step} style={{ display: 'flex', gap: '10px', alignItems: 'center', fontSize: '0.82rem' }}>
                  <span style={{
                    width: 18,
                    height: 18,
                    borderRadius: '50%',
                    display: 'grid',
                    placeItems: 'center',
                    border: '1px solid var(--border-subtle)',
                    color: index === 2 ? 'var(--accent)' : 'var(--fg-muted)',
                    fontSize: '11px',
                    flex: 'none'
                  }}>{index + 1}</span>
                  <span style={{ color: index === 2 ? 'var(--fg)' : 'var(--fg-subtle)' }}>{step}</span>
                </div>
              ))}
            </div>
            <div style={{ marginTop: '18px', background: 'var(--bg-subtle)', borderRadius: '10px', height: '8px', overflow: 'hidden' }}>
              <div className="gemma-loading-bar" />
            </div>
          </div>
        ) : generatingProgress && (
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
