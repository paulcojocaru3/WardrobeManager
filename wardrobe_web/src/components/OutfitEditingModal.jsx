import React, { useState, useCallback, useMemo } from 'react';
import Button from './Button';
import Modal from './Modal';
import { CLOTHING_TYPES } from '../constants/wardrobe';
import './OutfitEditingModal.css';

/**
 * OutfitEditingModal - Component for editing outfit for a specific day in a planner event
 * Allows users to:
 * - Select from existing outfits
 * - Edit individual items by adding/removing clothes
 * - Save changes via UpdateEventItineraryCommand
 */
const OutfitEditingModal = ({
  isOpen,
  onClose,
  onSave,
  clothes,
  outfits,
  currentOutfit,
  dayInfo,
  loading,
  mode = 'edit', // 'edit' or 'plan'
  initialMoment = '',
}) => {
  // Tab state: 'select' or 'edit'
  const [tab, setTab] = useState('select');
  
  // Selected outfit when in 'select' tab
  const [selectedOutfitId, setSelectedOutfitId] = useState(currentOutfit?.id || null);
  
  // Edited items when in 'edit' tab
  const [editedItemIds, setEditedItemIds] = useState(currentOutfit?.items?.map(i => i.id) || []);

  // Track which clothing items to show (filtered by clothing type for better UX)
  const [clothingTypeFilter, setClothingTypeFilter] = useState(0);

  // Moment state for planning
  const [moment, setMoment] = useState(initialMoment);

  // Update state when modal opens or currentOutfit changes
  React.useEffect(() => {
    if (isOpen) {
      setSelectedOutfitId(currentOutfit?.id || null);
      setEditedItemIds(currentOutfit?.items?.map(i => i.id) || []);
      setTab('select');
      setClothingTypeFilter(0);
      setMoment(initialMoment);
    }
  }, [isOpen, currentOutfit, initialMoment]);

  // Filter clothes based on selected type
  const filteredClothes = useMemo(() => {
    return clothes.filter(c => c.type === clothingTypeFilter);
  }, [clothes, clothingTypeFilter]);

  // Get outfit preview items
  const previewOutfit = useMemo(() => {
    if (tab === 'select' && selectedOutfitId) {
      return outfits.find(o => o.id === selectedOutfitId);
    }
    // In edit mode, reconstruct outfit from selected item IDs
    const items = clothes.filter(c => editedItemIds.includes(c.id));
    return { items, name: 'Custom Outfit' };
  }, [tab, selectedOutfitId, editedItemIds, outfits, clothes]);

  // Handle toggle item selection in edit mode
  const handleToggleItem = useCallback((item) => {
    setEditedItemIds(prev => {
      if (prev.includes(item.id)) {
        return prev.filter(id => id !== item.id);
      } else {
        const sameType = clothes.find(c => prev.includes(c.id) && c.type === item.type);
        return sameType ? [...prev.filter(id => id !== sameType.id), item.id] : [...prev, item.id];
      }
    });
  }, [clothes]);

  // Handle save button
  const handleSave = useCallback(() => {
    if (tab === 'select') {
      // Save with selected outfit
      onSave({
        outfitId: selectedOutfitId,
        itemIds: null, // Use outfit as-is
        moment: mode === 'plan' ? moment : undefined,
      });
    } else {
      // Save with edited items
      onSave({
        outfitId: null,
        itemIds: editedItemIds,
        moment: mode === 'plan' ? moment : undefined,
      });
    }
  }, [tab, selectedOutfitId, editedItemIds, moment, mode, onSave]);

  const handleReset = useCallback(() => {
    setTab('select');
    setSelectedOutfitId(currentOutfit?.id || null);
    setEditedItemIds(currentOutfit?.items?.map(i => i.id) || []);
    setClothingTypeFilter(null);
    setMoment(initialMoment);
  }, [currentOutfit, initialMoment]);

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Edit Outfit - ${dayInfo?.label || 'Day'}`} size="large">
      <div className="outfit-editing-modal">
        {/* Tab Navigation */}
        <div className="modal-tabs">
          <button
            className={`modal-tab ${tab === 'select' ? 'active' : ''}`}
            onClick={() => {
              setTab('select');
              setClothingTypeFilter(null);
            }}
          >
            SELECT OUTFIT
          </button>
          <button
            className={`modal-tab ${tab === 'edit' ? 'active' : ''}`}
            onClick={() => {
              setTab('edit');
              setClothingTypeFilter(null);
            }}
          >
            EDIT ITEMS
          </button>
        </div>

        {/* Tab Content */}
        <div className="modal-tab-content">
          {tab === 'select' ? (
            // SELECT TAB - Show available outfits
            <div className="select-outfit-tab">
              {outfits.length === 0 ? (
                <div className="empty-state">
                  <p>No outfits available. Create one first!</p>
                </div>
              ) : (
                <div className="outfits-select-grid" style={{ maxHeight: '400px', overflowY: 'auto', padding: '10px', border: '1px solid var(--border-subtle)', borderRadius: '15px' }}>
                  {outfits.map(outfit => (
                    <div
                      key={outfit.id}
                      className={`outfit-select-card ${selectedOutfitId === outfit.id ? 'selected' : ''}`}
                      onClick={() => setSelectedOutfitId(outfit.id)}
                    >
                      <div className="outfit-select-items">
                        {outfit.items?.slice(0, 4).map(item => (
                          <div key={item.id} className="select-item-thumb">
                            <img src={item.processedImageUrl} alt={item.name} title={item.name} />
                          </div>
                        ))}
                      </div>
                      <div className="outfit-select-name">{outfit.name}</div>
                      <div className="outfit-select-count">{outfit.items?.length || 0} items</div>
                      {selectedOutfitId === outfit.id && <div className="select-badge">✓</div>}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ) : (
            // EDIT TAB - Show clothing items for manual selection
            <div className="edit-items-tab">
              {/* Type Filter */}
              <div className="type-filter" style={{ display: 'flex', gap: '10px', overflowX: 'auto', paddingBottom: '10px', marginBottom: '10px' }}>
                {CLOTHING_TYPES.map((type, idx) => (
                  <button
                    key={type}
                    className={`filter-btn ${clothingTypeFilter === idx ? 'active' : ''}`}
                    onClick={() => setClothingTypeFilter(idx)}
                    style={{
                      padding: '8px 16px',
                      borderRadius: '20px',
                      border: 'none',
                      background: clothingTypeFilter === idx ? 'var(--accent-bg)' : 'var(--bg-raised)',
                      color: clothingTypeFilter === idx ? 'var(--accent-fg)' : 'var(--fg-muted)',
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

              {/* Items Grid */}
              <div className="items-edit-grid" style={{ maxHeight: '400px', overflowY: 'auto', padding: '10px', border: '1px solid var(--border-subtle)', borderRadius: '15px' }}>
                {filteredClothes.length === 0 ? (
                  <div className="empty-state">
                    <p>No clothing items available in this category</p>
                  </div>
                ) : (
                  filteredClothes.map(item => {
                    const isSelected = editedItemIds.includes(item.id);
                    return (
                      <div
                        key={item.id}
                        className={`edit-item-card ${isSelected ? 'selected' : ''}`}
                        onClick={() => handleToggleItem(item)}
                      >
                        <img src={item.processedImageUrl} alt={item.name} />
                        <div className="item-select-indicator">
                          {isSelected ? '✓' : '+'}
                        </div>
                        <div className="item-edit-name">{item.name}</div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}
        </div>

        {/* Preview Section */}
        <div className="modal-preview">
          <h4 className="preview-title">Preview</h4>
          {previewOutfit?.items && previewOutfit.items.length > 0 ? (
            <div className="preview-items">
              {previewOutfit.items.map(item => (
                <div key={item.id} className="preview-item">
                  <img src={item.processedImageUrl} alt={item.name} title={item.name} />
                  <span className="preview-item-name">{item.name}</span>
                </div>
              ))}
            </div>
          ) : (
            <div className="preview-empty">
              <p>No items selected</p>
            </div>
          )}
        </div>

        {/* Moment Input for Planning */}
        {mode === 'plan' && (
          <div style={{ padding: '15px 20px', borderTop: '1px solid var(--border-subtle)', textAlign: 'left' }}>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>MOMENT (e.g. Morning, Dinner, Flight)</span>
            <input 
              type="text" 
              className="name-input" 
              value={moment} 
              onChange={e => setMoment(e.target.value)}
              placeholder="Enter moment..."
              style={{ width: '100%' }}
            />
          </div>
        )}

        {/* Action Buttons */}
        <div className="modal-actions">
          <button
            className="gen-btn"
            onClick={handleSave}
            disabled={loading || (tab === 'select' ? !selectedOutfitId : editedItemIds.length === 0) || (mode === 'plan' && !moment.trim())}
            style={{
              opacity: loading || (tab === 'select' ? !selectedOutfitId : editedItemIds.length === 0) || (mode === 'plan' && !moment.trim()) ? 0.5 : 1,
              cursor: loading ? 'not-allowed' : 'pointer',
            }}
          >
            {loading ? 'SAVING...' : (mode === 'plan' ? 'PLAN OUTFIT' : 'SAVE OUTFIT')}
          </button>
          <button
            className="close-link"
            onClick={() => {
              handleReset();
              onClose();
            }}
            disabled={loading}
          >
            CANCEL
          </button>
        </div>
      </div>
    </Modal>
  );
};

export default OutfitEditingModal;
