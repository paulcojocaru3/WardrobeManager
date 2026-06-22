import Modal from '../Modal';
import Button from '../Button';
import { CLOTHING_TYPES } from '../../constants/wardrobe';

const MIN_ALTERNATIVE_SCORE = 0.15;

const buildSlots = (aiData) =>
  (aiData.recommendationsPerType || [])
    .map((rec) => {
      const selected = aiData.selectedItems.find((si) =>
        (rec.topCandidates || []).some((tc) => tc.id === si.id));
      const alternatives = (rec.topCandidates || [])
        .filter((candidate) => !selected || candidate.id !== selected.id)
        .filter((candidate) => candidate.similarityScore > MIN_ALTERNATIVE_SCORE)
        .slice(0, 3);

      return {
        type: rec.type,
        label: CLOTHING_TYPES[rec.type] ?? 'Item',
        selectedId: selected?.id,
        alternatives,
      };
    })
    .filter((slot) => slot.alternatives.length > 0);

const AiSuggestionModal = ({
  isOpen,
  onClose,
  aiData,
  setAiData,
  stylingNotes,
  notesLoading,
  insight,
  onSaveAiOutfit,
  onRegenerate,
  loading,
}) => {
  const slots = aiData ? buildSlots(aiData) : [];
  const hasInsight = !!insight && (insight.headline || (insight.items || []).length > 0 || insight.weatherAdvice);
  const stylistHighlights = aiData?.stylistHighlights || aiData?.StylistHighlights || [];
  const stylistHeadline = aiData?.stylistHeadline || aiData?.StylistHeadline;
  const stylistTip = aiData?.stylistTip || aiData?.StylistTip;
  const generatedByStylist = aiData?.generatedByStylist || aiData?.GeneratedByStylist;
  const hasStylistPanel = generatedByStylist && (stylistHeadline || stylistTip || stylistHighlights.length > 0);

  const slotSelectedIds = new Set(slots.map((slot) => slot.selectedId).filter(Boolean));
  const scoreById = new Map(
    (aiData?.recommendationsPerType || [])
      .flatMap((rec) => rec.topCandidates || [])
      .map((candidate) => [candidate.id, candidate.similarityScore]));

  const scoreLabel = (value) => value == null ? null : `${Math.round(value * 100)}%`;
  const typeLabel = (item) => {
    const type = item?.type;
    if (typeof type === 'number') return CLOTHING_TYPES[type] || 'Item';
    return type || 'Item';
  };

  const swapItem = (slot, candidate) => {
    if (!slot.selectedId) return;

    setAiData({
      ...aiData,
      selectedItems: aiData.selectedItems.map((item) =>
        item.id === slot.selectedId ? candidate : item),
    });
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Outfit review"
      size="large"
      contentClassName="ai-outfit-modal-shell"
    >
      {aiData && (
        <div className="ai-outfit-review">
          <header className="ai-review-header">
            <div>
              <span className="ai-review-kicker">Generated look</span>
              <input
                className="ai-review-name-input"
                value={aiData.name}
                onChange={(event) => setAiData({ ...aiData, name: event.target.value })}
                aria-label="Outfit name"
              />
            </div>
            <div className="ai-review-count">
              <strong>{aiData.selectedItems.length}</strong>
              <span>items</span>
            </div>
          </header>

          <div className="ai-review-grid">
            <section className="ai-look-panel">
              <div className="ai-look-canvas">
                {aiData.selectedItems.map((item, index) => {
                  const score = slotSelectedIds.has(item.id) ? scoreById.get(item.id) : null;

                  return (
                    <div key={item.id} className={`ai-look-item${index === 0 ? ' is-main' : ''}`}>
                      <img src={item.processedImageUrl} alt={item.name || typeLabel(item)} />
                      <div className="ai-look-item-meta">
                        <span>{typeLabel(item)}</span>
                        {score != null && <strong>{scoreLabel(score)}</strong>}
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>

            <aside className="ai-notes-rail">
              {hasInsight ? (
                <section className="ai-note-card ai-note-card-main">
                  <span className="ai-review-kicker">Style notes</span>
                  {insight.headline && <h4>{insight.headline}</h4>}
                  {insight.weatherAdvice && <p className="ai-weather-note">{insight.weatherAdvice}</p>}
                  {(insight.items || []).length > 0 && (
                    <div className="ai-item-notes">
                      {insight.items.map((item, index) => (
                        <div key={index} className="ai-item-note">
                          <span>{item.slot || 'item'}</span>
                          <p>{item.note}</p>
                        </div>
                      ))}
                    </div>
                  )}
                </section>
              ) : (notesLoading || (stylingNotes || []).length > 0) && (
                <section className="ai-note-card">
                  <span className="ai-review-kicker">Style notes</span>
                  {notesLoading ? (
                    <p className="ai-muted-copy">Composing notes...</p>
                  ) : (
                    <div className="ai-simple-notes">
                      {stylingNotes.map((note, index) => <p key={index}>{note}</p>)}
                    </div>
                  )}
                </section>
              )}

              {hasStylistPanel && (
                <section className="ai-note-card">
                  <span className="ai-review-kicker">Gemma3 final pick</span>
                  {stylistHeadline && <h4>{stylistHeadline}</h4>}
                  {stylistHighlights.length > 0 && (
                    <div className="ai-simple-notes">
                      {stylistHighlights.map((highlight, index) => <p key={index}>{highlight}</p>)}
                    </div>
                  )}
                  {stylistTip && <p className="ai-weather-note">{stylistTip}</p>}
                </section>
              )}

              {(aiData.warnings || []).length > 0 && (
                <section className="ai-note-card ai-warning-card">
                  <span className="ai-review-kicker">Constraints relaxed</span>
                  <div className="ai-simple-notes">
                    {aiData.warnings.map((warning, index) => <p key={index}>{warning}</p>)}
                  </div>
                </section>
              )}
            </aside>
          </div>

          {slots.length > 0 && (
            <section className="ai-swap-section">
              <div className="ai-swap-header">
                <span className="ai-review-kicker">Alternatives</span>
                <p>Tap an item to replace the current pick in that slot.</p>
              </div>
              <div className="ai-swap-groups">
                {slots.map((slot) => (
                  <div key={slot.label} className="ai-swap-group">
                    <div className="ai-swap-slot">{slot.label}</div>
                    <div className="ai-swap-list">
                      {slot.alternatives.map((candidate) => (
                        <button
                          key={candidate.id}
                          className="ai-swap-card"
                          onClick={() => swapItem(slot, candidate)}
                          title="Swap into the outfit"
                        >
                          <img src={candidate.processedImageUrl} alt={candidate.name || slot.label} />
                          <span>{scoreLabel(candidate.similarityScore)}</span>
                        </button>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          <footer className="ai-review-actions">
            <Button label="Confirm & save" onClick={onSaveAiOutfit} loading={loading} />
            {onRegenerate && (
              <Button label="Generate another" variant="secondary" onClick={onRegenerate} loading={loading} />
            )}
            <Button label="Discard" variant="secondary" onClick={onClose} />
          </footer>
        </div>
      )}
    </Modal>
  );
};

export default AiSuggestionModal;
