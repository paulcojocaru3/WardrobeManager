import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import Button from '../components/Button';
import Modal from '../components/Modal';

const API_BASE_URL = 'http://localhost:5150/api'; 
const CLOTHING_TYPES = ["TOP", "BOTTOM", "SHOES", "OUTERWEAR", "ACCESSORIES"];

const DashboardPage = ({ user, onLogout }) => {
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('clothes');
  const [selectedItem, setSelectedItem] = useState(null);
  
  const [uploadModal, setUploadModal] = useState(false);
  const [uploadData, setUploadData] = useState({ file: null, name: '' });
  
  const [editModal, setEditModal] = useState(false);
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [] });

  const fileInputRef = useRef(null);
  const userId = user?.id || user?.Id;

  const fetchClothes = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/clothing/${userId}`);
      console.log("CLOTHES:", res.data);
      setClothes(Array.isArray(res.data) ? res.data : []);
    } catch (e) { console.error("Clothes error:", e); }
  };

  const fetchOutfits = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/outfits/user/${userId}`);
      console.log("OUTFITS:", res.data);
      setOutfits(Array.isArray(res.data) ? res.data : []);
    } catch (e) { console.error("Outfits error:", e); }
  };

  const refresh = () => {
    if (!userId) return;
    fetchClothes();
    fetchOutfits();
  };

  useEffect(() => { refresh(); }, [userId]);

  const onUpload = async () => {
    setLoading(true);
    const fd = new FormData();
    fd.append('File', uploadData.file);
    fd.append('UserId', userId);
    fd.append('Name', uploadData.name);
    try {
      await axios.post(`${API_BASE_URL}/clothing/upload`, fd);
      setUploadModal(false);
      fetchClothes();
    } catch (err) { alert("Upload failed"); }
    finally { setLoading(false); }
  };

  const onGenerate = async () => {
    setLoading(true);
    try {
      await axios.post(`${API_BASE_URL}/outfits/generate`, { userId, startItemId: selectedItem.id });
      setSelectedItem(null);
      setView('outfits');
      fetchOutfits();
    } catch (err) { alert("Generation failed"); }
    finally { setLoading(false); }
  };

  const onEditSave = async () => {
    setLoading(true);
    try {
      await axios.put(`${API_BASE_URL}/outfits/${editData.id}`, editData);
      setEditModal(false);
      fetchOutfits();
    } catch (err) { alert("Update failed"); }
    finally { setLoading(false); }
  };

  const onDelete = async (type, id) => {
    try {
      const endpoint = type === 'cloth' ? `clothing/${id}` : `outfits/${id}`;
      await axios.delete(`${API_BASE_URL}/${endpoint}`);
      type === 'cloth' ? fetchClothes() : fetchOutfits();
    } catch (err) { alert("Delete failed"); }
  };

  return (
    <div className="desktop-wrapper">
      <aside className="side-nav">
        <div className="brand">W.</div>
        <div className="nav-links">
          <button className={`nav-btn ${view === 'clothes' ? 'active' : ''}`} onClick={() => setView('clothes')}>clothes</button>
          <button className={`nav-btn ${view === 'outfits' ? 'active' : ''}`} onClick={() => setView('outfits')}>outfits</button>
        </div>
        <button className="exit-circle" onClick={onLogout}></button>
      </aside>

      <main className="stage">
        <div className="centered-content">
          <h2 className="soft-title">{view === 'clothes' ? 'your wardrobe' : 'generated outfits'}</h2>
          
          {view === 'clothes' ? (
            <div className="wardrobe-container">
              <div className="upload-section">
                <div className="empty-state-card" onClick={() => fileInputRef.current.click()}>+ ADD NEW ITEM</div>
              </div>
              {CLOTHING_TYPES.map((typeName, typeIndex) => {
                const filtered = clothes.filter(i => i.type === typeIndex || i.type === typeName);
                if (filtered.length === 0) return null;
                return (
                  <div key={typeName} className="category-section">
                    <h3 className="category-title">{typeName === 'ACCESSORY' ? 'ACCESSORIES' : typeName}</h3>
                    <div className="clothes-grid">
                      {filtered.map(item => (
                        <div key={item.id} className="item-card" onClick={() => setSelectedItem(item)}>
                          <button className="delete-trigger" onClick={(e) => { e.stopPropagation(); onDelete('cloth', item.id); }}>remove</button>
                          <img src={item.processedImageUrl} alt="" />
                          <span className="item-name-tag">{item.name}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="outfits-list">
              {outfits.map(o => (
                <div key={o.id} className="outfit-row">
                  <div className="outfit-info">
                    <div className="outfit-header-left">
                      <span className="outfit-name">{o.name}</span>
                      <button className="edit-mini-btn" onClick={() => {
                        setEditData({ id: o.id, name: o.name, itemIds: o.items?.map(i => i.id) || [] });
                        setEditModal(true);
                      }}>edit items</button>
                    </div>
                    <div className="outfit-actions">
                      <span className="outfit-date">{new Date(o.createdAt).toLocaleDateString()}</span>
                      <Button label="remove" variant="danger" onClick={() => onDelete('outfit', o.id)} />
                    </div>
                  </div>
                  <div className="outfit-items-preview">
                    {o.items && o.items.map(i => (
                      <div key={i.id} className="mini-item clickable" onClick={() => setSelectedItem(i)}>
                        <img src={i.processedImageUrl} alt="" title={i.name} />
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
          <input type="file" ref={fileInputRef} onChange={(e) => {
            const file = e.target.files[0];
            if (file) { setUploadData({ file, name: file.name.split('.')[0] }); setUploadModal(true); }
          }} accept="image/*" hidden />
        </div>
      </main>

      <Modal isOpen={uploadModal} onClose={() => setUploadModal(false)} title="Set Name" size="small">
        <input className="name-input" value={uploadData.name} onChange={e => setUploadData({...uploadData, name: e.target.value})} autoFocus />
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <Button label="Confirm" onClick={onUpload} loading={loading} />
          <Button label="Cancel" variant="secondary" onClick={() => setUploadModal(false)} />
        </div>
      </Modal>

      <Modal isOpen={editModal} onClose={() => setEditModal(false)} title="Edit Outfit" size="large">
        <div className="edit-outfit-container">
          <input className="name-input" value={editData.name} onChange={e => setEditData({...editData, name: e.target.value})} />
          <div className="edit-items-grid">
            {clothes.map(item => {
              const isSelected = editData.itemIds.includes(item.id);
              return (
                <div key={item.id} className={`selectable-item ${isSelected ? 'selected' : ''}`} onClick={() => {
                  if (isSelected) setEditData({...editData, itemIds: editData.itemIds.filter(id => id !== item.id)});
                  else {
                    const sameType = clothes.find(c => editData.itemIds.includes(c.id) && c.type === item.type);
                    const newIds = sameType ? [...editData.itemIds.filter(id => id !== sameType.id), item.id] : [...editData.itemIds, item.id];
                    setEditData({...editData, itemIds: newIds});
                  }
                }}>
                  <img src={item.processedImageUrl} alt="" />
                  <div className="check-badge">{isSelected ? '✓' : '+'}</div>
                </div>
              );
            })}
          </div>
          <Button label="Save Outfit" onClick={onEditSave} loading={loading} />
        </div>
      </Modal>

      <Modal isOpen={!!selectedItem} onClose={() => setSelectedItem(null)} title={selectedItem?.name}>
        {selectedItem && <>
          <div className="inspect-header">
            <span className="robotic-text">TYPE: {CLOTHING_TYPES[selectedItem.type] || selectedItem.type}</span>
            <span className="robotic-text">COLOR: {selectedItem.color?.toUpperCase()}</span>
          </div>
          <img src={selectedItem.processedImageUrl} alt="" style={{ width: '100%', borderRadius: '20px', marginBottom: '20px' }} />
          <div className="modal-actions">
            <Button label="Generate Outfit" onClick={onGenerate} loading={loading} />
            <Button label="Close" variant="secondary" onClick={() => setSelectedItem(null)} />
          </div>
        </>}
      </Modal>
    </div>
  );
};

export default DashboardPage;
