import Modal from '../Modal';
import { USAGES } from '../../constants/wardrobe';

const StyleSelectionModal = ({
  isOpen,
  onClose,
  executeGeneration,
  isRediscover = false,
  preferUnused = false,
  setPreferUnused,
  useGemmaStylistForOutfits = false,
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isRediscover ? 'REDISCOVER — PICK A STYLE' : 'SELECT OUTFIT STYLE'} size="medium">
      <div style={{ padding: '10px' }}>
        {isRediscover && (
          <p style={{ margin: '0 0 14px', opacity: 0.7, fontSize: '0.85rem' }}>
            We'll build the outfit around a piece you rarely or never wear.
          </p>
        )}
        {useGemmaStylistForOutfits && (
          <div style={{
            margin: '0 0 14px',
            padding: '12px 14px',
            border: '1px solid var(--border-subtle)',
            borderRadius: '10px',
            background: 'var(--bg-soft)',
            fontSize: '0.85rem',
            lineHeight: 1.5,
            color: 'var(--fg-subtle)'
          }}>
            Gemma3 will choose the final outfit before it opens. This takes longer, but avoids showing one outfit first and replacing it later.
          </div>
        )}

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

        {!isRediscover && setPreferUnused && (
          <label style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '18px', cursor: 'pointer', fontSize: '0.85rem', opacity: 0.85 }}>
            <input
              type="checkbox"
              checked={preferUnused}
              onChange={(e) => setPreferUnused(e.target.checked)}
            />
            Prefer items I rarely wear
          </label>
        )}
      </div>
    </Modal>
  );
};

export default StyleSelectionModal;
