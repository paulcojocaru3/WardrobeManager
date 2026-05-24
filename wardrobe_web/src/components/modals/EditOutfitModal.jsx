import React, { useState } from 'react';
import Modal from '../Modal';
import { CLOTHING_TYPES } from '../../constants/wardrobe';

const PRESET_TAGS = ['Work', 'Outside', 'Gym', 'Casual', 'Party', 'Travel', 'Date Night', 'Formal', 'Lounge', 'Beach'];

const EditOutfitModal = ({ isOpen, onClose, editData, setEditData, clothes, onEditSave, loading }) => {
  const [categoryFilter, setCategoryFilter] = useState('ALL');
  const [tagInput, setTagInput] = useState('');

  const selectedItems = clothes.filter(c => editData.itemIds.includes(c.id));

  const toggleItem = (item) => {
    const isSelected = editData.itemIds.includes(item.id);
    if (isSelected) {
      setEditData({ ...editData, itemIds: editData.itemIds.filter(id => id !== item.id) });
    } else {
      const sameType = clothes.find(c => editData.itemIds.includes(c.id) && c.type === item.type);
      const newIds = sameType
        ? [...editData.itemIds.filter(id => id !== sameType.id), item.id]
        : [...editData.itemIds, item.id];
      setEditData({ ...editData, itemIds: newIds });
    }
  };

  const addTag = (tag) => {
    const trimmed = tag.trim();
    if (!trimmed) return;
    const tags = editData.tags || [];
    if (tags.map(t => t.toLowerCase()).includes(trimmed.toLowerCase())) return;
    setEditData({ ...editData, tags: [...tags, trimmed] });
  };

  const removeTag = (tag) => {
    setEditData({ ...editData, tags: (editData.tags || []).filter(t => t !== tag) });
  };

  const handleTagKeyDown = (e) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      addTag(tagInput);
      setTagInput('');
    }
  };

  const filteredClothes = categoryFilter === 'ALL'
    ? clothes
    : clothes.filter(c => {
        const typeStr = typeof c.type === 'number' ? CLOTHING_TYPES[c.type] : c.type;
        return typeStr === categoryFilter;
      });

  const categories = ['ALL', ...CLOTHING_TYPES];
  const currentTags = editData.tags || [];

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="large">
      <div className="eom-root">

        {/* Name field */}
        <input
          className="eom-name-input"
          value={editData.name}
          onChange={e => setEditData({ ...editData, name: e.target.value })}
          placeholder="Outfit name…"
        />

        {/* Selected items strip */}
        <div>
          <div className="eom-section-header">
            <span className="eom-section-label">Selected</span>
            <span className="eom-section-count">{selectedItems.length} items</span>
          </div>
          <div className="eom-selected-strip">
            {selectedItems.length === 0 ? (
              <span className="eom-empty-strip">No items selected — pick from the grid below</span>
            ) : (
              selectedItems.map(item => (
                <div key={item.id} className="eom-chip" title={item.name}>
                  <img src={item.processedImageUrl} alt={item.name} />
                  <button
                    className="eom-chip-x"
                    onClick={e => { e.stopPropagation(); toggleItem(item); }}
                  >×</button>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Outfit tags */}
        <div>
          <div className="eom-section-header">
            <span className="eom-section-label">Tags</span>
            <span className="eom-section-count">{currentTags.length} added</span>
          </div>
          <div className="eom-tags-area">
            <div className="eom-tags-row">
              {currentTags.map(tag => (
                <span key={tag} className="eom-tag-chip">
                  {tag}
                  <button className="eom-tag-x" onClick={() => removeTag(tag)}>×</button>
                </span>
              ))}
              <input
                className="eom-tag-input"
                value={tagInput}
                onChange={e => setTagInput(e.target.value)}
                onKeyDown={handleTagKeyDown}
                placeholder="Add tag…"
              />
            </div>
            <div className="eom-tag-presets">
              {PRESET_TAGS.filter(t => !currentTags.map(x => x.toLowerCase()).includes(t.toLowerCase())).map(t => (
                <button key={t} className="eom-preset-tag" onClick={() => addTag(t)}>{t}</button>
              ))}
            </div>
          </div>
        </div>

        {/* Category filter + items grid */}
        <div className="eom-wardrobe-section">
          <div className="eom-section-header">
            <span className="eom-section-label">Wardrobe</span>
            <span className="eom-section-count">{filteredClothes.length} items</span>
          </div>
          <div className="eom-category-tabs">
            {categories.map(cat => (
              <button
                key={cat}
                className={`eom-cat-tab${categoryFilter === cat ? ' on' : ''}`}
                onClick={() => setCategoryFilter(cat)}
              >{cat}</button>
            ))}
          </div>
          <div className="eom-items-grid">
            {filteredClothes.map(item => {
              const isSelected = editData.itemIds.includes(item.id);
              const typeLabel = typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type;
              return (
                <div
                  key={item.id}
                  className={`eom-item${isSelected ? ' sel' : ''}`}
                  onClick={() => toggleItem(item)}
                  title={item.name}
                >
                  <img src={item.processedImageUrl} alt={item.name} />
                  <span className="eom-item-name">{typeLabel}</span>
                  <span className="eom-item-tick">✓</span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Footer */}
        <div className="eom-footer">
          <span className="eom-count-label">
            {selectedItems.length} item{selectedItems.length !== 1 ? 's' : ''} selected
          </span>
          <button className="sw-btn ghost" onClick={onClose}>Cancel</button>
          <button
            className="sw-btn accent"
            onClick={onEditSave}
            disabled={loading || selectedItems.length === 0}
          >
            {loading ? 'Saving…' : 'Save outfit'}
          </button>
        </div>

      </div>
    </Modal>
  );
};

export default EditOutfitModal;
