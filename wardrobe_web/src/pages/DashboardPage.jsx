import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import Button from '../components/Button';
import Modal from '../components/Modal';
import StatsSection from '../components/StatsSection';

const API_BASE_URL = 'http://localhost:5150/api'; 
const CLOTHING_TYPES = ["TOP", "BOTTOM", "SHOES", "OUTERWEAR", "ACCESSORY"];
const GENDERS = ["Men", "Women", "Boys", "Girls", "Unisex"];
const SEASONS = ["Summer", "Fall", "Winter", "Spring"];
const USAGES = ["Casual", "Ethnic", "Formal", "Party", "Smart Casual", "Sports", "Travel"];
const COLORS = [
  "black", "white", "off-white", "cream", "beige", "ivory", "silver", "grey", "charcoal", "dark grey",
  "navy blue", "royal blue", "sky blue", "baby blue", "azure", "teal", "turquoise", "cyan",
  "dark green", "emerald green", "olive green", "lime green", "mint green", "forest green", "khaki",
  "maroon", "burgundy", "ruby red", "crimson", "scarlet", "brick red", "terracotta",
  "hot pink", "baby pink", "fuchsia", "magenta", "rose", "coral", "salmon",
  "purple", "violet", "lavender", "plum", "eggplant", "mauve", "lilac",
  "golden", "yellow", "mustard", "lemon", "amber", "gold",
  "orange", "tangerine", "peach", "apricot", "burnt orange",
  "brown", "chocolate", "tan", "camel", "caramel", "coffee", "bronze", "copper",
  "denim blue", "washed blue", "indigo", "violet", "wine red", "sand", "taupe"
];

const DashboardPage = ({ user, onLogout }) => {
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('clothes');
  const [selectedItem, setSelectedItem] = useState(null);
  const [editItemMode, setEditItemMode] = useState(false);
  const [editItemData, setEditItemData] = useState(null);
  
  const [uploadModal, setUploadModal] = useState(false);
  const [uploadData, setUploadData] = useState({ file: null, name: '' });
  
  const [validationModal, setValidationModal] = useState(false);
  const [validationData, setValidationData] = useState(null);
  const [originalPredictions, setOriginalPredictions] = useState(null);
  const [currentStep, setCurrentStep] = useState(0); 
  const [validationSearchTerm, setValidationSearchTerm] = useState('');
  
  const [editModal, setEditModal] = useState(false);
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [] });

  const [aiModal, setAiModal] = useState(false);
  const [aiData, setAiData] = useState(null);
  
  // Custom Outfit State
  const [customOutfitModal, setCustomOutfitModal] = useState(false);
  const [customOutfitData, setCustomOutfitData] = useState({ name: '', itemIds: [] });
  const [customOutfitTab, setCustomOutfitTab] = useState(0); 

  const [city, setCity] = useState(localStorage.getItem('userCity') || 'Detecting...');
  const [cityModal, setCityModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [weatherInfo, setWeatherInfo] = useState(null);
  const [citySuggestions, setCitySuggestions] = useState([]);
  const [styleSelectionModal, setStyleSelectionModal] = useState(false);
  const [generationContext, setGenerationContext] = useState(null); 

  const fileInputRef = useRef(null);
  const userId = user?.id || user?.Id;

  useEffect(() => {
    const searchCities = async () => {
      if (!searchTerm || searchTerm.length < 3) {
        setCitySuggestions([]);
        return;
      }
      try {
        const res = await axios.get(`${API_BASE_URL}/outfits/cities/search?query=${searchTerm}`);
        setCitySuggestions(res.data);
      } catch (e) { console.error("City search error", e); }
    };

    const timeoutId = setTimeout(searchCities, 400); 
    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  useEffect(() => {
    const detectLocation = async () => {
      const savedCity = localStorage.getItem('userCity');
      if (savedCity) {
        setCity(savedCity);
        return;
      }

      try {
        const res = await axios.get('https://ipapi.co/json/');
        if (res.data && res.data.city) {
          setCity(res.data.city);
          localStorage.setItem('userCity', res.data.city);
        } else {
          throw new Error("Invalid response from ipapi");
        }
      } catch (e) {
        try {
          const res = await axios.get('http://ip-api.com/json/');
          if (res.data && res.data.city) {
            setCity(res.data.city);
            localStorage.setItem('userCity', res.data.city);
          } else {
            setCity('Bucharest');
          }
        } catch (e2) {
          setCity('Bucharest');
        }
      }
    };
    detectLocation();
  }, []);

  const handleCityChange = (newCity) => {
    setCity(newCity);
    localStorage.setItem('userCity', newCity);
  };

  const fetchWeather = async () => {
    if (city === 'Detecting...') return;
    try {
      const res = await axios.get(`${API_BASE_URL}/outfits/weather/${city}`);
      setWeatherInfo(res.data);
    } catch (e) { console.error("Weather error:", e); }
  };

  const getWeatherStyles = () => {
    if (!weatherInfo || !weatherInfo.condition) return { background: 'rgba(255,255,255,0.05)', color: '#fff' };
    
    const condition = weatherInfo.condition.toLowerCase();
    if (condition.includes('clear') || condition.includes('sun')) 
      return { background: 'linear-gradient(135deg, #FF8C00 0%, #FFD700 100%)', color: '#000' };
    if (condition.includes('cloud')) 
      return { background: 'linear-gradient(135deg, #757F9A 0%, #D7DDE8 100%)', color: '#000' };
    if (condition.includes('rain') || condition.includes('drizzle')) 
      return { background: 'linear-gradient(135deg, #2c3e50 0%, #4ca1af 100%)', color: '#fff' };
    if (condition.includes('snow')) 
      return { background: 'linear-gradient(135deg, #E0EAFC 0%, #CFDEF3 100%)', color: '#000' };
    
    return { background: 'linear-gradient(135deg, #646cff 0%, #9089ff 100%)', color: '#fff' };
  };

  const fetchClothes = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/clothing/${userId}`);
      setClothes(Array.isArray(res.data) ? res.data : []);
    } catch (e) { console.error("Clothes error:", e); }
  };

  const fetchOutfits = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/outfits/user/${userId}`);
      setOutfits(Array.isArray(res.data) ? res.data : []);
    } catch (e) { console.error("Outfits error:", e); }
  };

  const refresh = () => {
    if (!userId) return;
    fetchClothes();
    fetchOutfits();
    fetchWeather();
  };

  useEffect(() => { refresh(); }, [userId]);
  useEffect(() => { fetchWeather(); }, [city]);

  const onGenerate = (item = null) => {
    if (item) setSelectedItem(item);
    setGenerationContext(item ? 'item' : 'today');
    setStyleSelectionModal(true);
  };

  const executeGeneration = async (style) => {
    setStyleSelectionModal(false);
    setLoading(true);
    
    let startItem = selectedItem;
    if (generationContext === 'today') {
      const candidates = clothes.filter(c => c.usage?.toLowerCase().includes(style.toLowerCase()));
      startItem = candidates.length > 0 
        ? candidates[Math.floor(Math.random() * candidates.length)] 
        : clothes[Math.floor(Math.random() * clothes.length)];
    }

    if (!startItem) { setLoading(false); return; }

    try {
      const { data } = await axios.post(`${API_BASE_URL}/outfits/generate-ai`, { 
        userId, 
        startItemId: startItem.id, 
        threshold: 0.5, 
        city, 
        style,
        season: weatherInfo?.seasonSuggestion 
      });
      setAiData(data);
      setAiModal(true);
    } catch (err) { alert("Generation failed"); }
    finally { setLoading(false); setSelectedItem(null); }
  };

  const onUpload = async () => {
    setLoading(true);
    const fd = new FormData();
    fd.append('File', uploadData.file);
    fd.append('UserId', userId);
    fd.append('Name', uploadData.name);
    try {
      const res = await axios.post(`${API_BASE_URL}/clothing/process`, fd);
      const data = res.data;
      
      setOriginalPredictions({
        type: data.type,
        color: data.color,
        gender: data.gender,
        season: data.season,
        usage: data.usage
      });

      setValidationData({
        ...data,
        season: data.season ? [data.season] : [],
        usage: data.usage ? [data.usage] : []
      });

      setCurrentStep(0);
      setUploadModal(false);
      setValidationModal(true);
    } catch (err) { alert("Processing failed"); }
    finally { setLoading(false); }
  };

  const onConfirmStep = () => {
    setValidationSearchTerm('');
    if (currentStep < 4) {
      setCurrentStep(currentStep + 1);
    } else {
      onSaveValidatedItem();
    }
  };

  const onSaveValidatedItem = async () => {
    setLoading(true);
    try {
      const payload = {
        userId,
        name: validationData.name,
        type: typeof validationData.type === 'string' ? CLOTHING_TYPES.indexOf(validationData.type.toUpperCase()) : validationData.type,
        color: validationData.color,
        gender: validationData.gender,
        season: Array.isArray(validationData.season) ? validationData.season.join(', ') : validationData.season,
        usage: Array.isArray(validationData.usage) ? validationData.usage.join(', ') : validationData.usage,
        processedImageB64: validationData.processedImageB64,
        embedding: validationData.embedding
      };
      
      if (payload.type === -1 && typeof validationData.type === 'number') {
          payload.type = validationData.type;
      }

      await axios.post(`${API_BASE_URL}/clothing/add`, payload);
      setValidationModal(false);
      fetchClothes();
    } catch (err) { alert("Save failed"); }
    finally { setLoading(false); }
  };

  const renderValidationStep = () => {
    if (!validationData || !originalPredictions) return null;

    const steps = [
      { label: 'TYPE', value: validationData.type, options: CLOTHING_TYPES, field: 'type', isEnum: true, original: originalPredictions.type },
      { label: 'COLOR', value: validationData.color, options: COLORS, field: 'color', isSearchable: true, original: originalPredictions.color },
      { label: 'GENDER', value: validationData.gender, options: GENDERS, field: 'gender', original: originalPredictions.gender },
      { label: 'SEASON', value: validationData.season, options: SEASONS, field: 'season', isMulti: true, original: originalPredictions.season },
      { label: 'USAGE', value: validationData.usage, options: USAGES, field: 'usage', isMulti: true, original: originalPredictions.usage }
    ];

    const step = steps[currentStep];

    const getSortedOptions = () => {
      const { options, original, isEnum } = step;
      let originalLabel = isEnum ? (typeof original === 'number' ? CLOTHING_TYPES[original] : original) : original;
      if (!originalLabel) return options;
      const cleanOriginal = String(originalLabel);
      const matchedOption = options.find(o => o.toLowerCase() === cleanOriginal.toLowerCase());
      if (!matchedOption) return options;
      return [matchedOption, ...options.filter(o => o.toLowerCase() !== matchedOption.toLowerCase())];
    };

    const sortedOptions = getSortedOptions();
    const filteredOptions = step.isSearchable 
      ? sortedOptions.filter(o => o.toLowerCase().includes(validationSearchTerm.toLowerCase()))
      : sortedOptions;

    return (
      <div className="validation-step-content" style={{ padding: '20px' }}>
        <div style={{ textAlign: 'center', marginBottom: '30px' }}>
           <img 
            src={`data:image/png;base64,${validationData.processedImageB64}`} 
            alt="Processed" 
            style={{ maxWidth: '100%', maxHeight: '250px', borderRadius: '20px', border: '1px solid #f5f5f5', padding: '10px', background: '#fcfcfc', objectFit: 'contain' }} 
          />
        </div>
        
        <div className="step-indicator" style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '30px' }}>
          {steps.map((_, i) => (
            <div key={i} style={{ width: '30px', height: '3px', background: i === currentStep ? '#000' : (i < currentStep ? '#eee' : '#f5f5f5'), borderRadius: '2px', transition: 'all 0.3s' }} />
          ))}
        </div>

        <div style={{ marginBottom: '30px', textAlign: 'left' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <span className="robotic-text" style={{ fontSize: '0.6rem', color: '#ccc' }}>STEP {currentStep + 1} OF 5: VERIFY {step.label}</span>
          </div>
          
          {step.isSearchable && (
            <div style={{ marginBottom: '15px' }}>
              <input type="text" placeholder={`Search...`} className="name-input" style={{ marginBottom: '10px', fontSize: '0.8rem', textAlign: 'left', padding: '10px 20px' }} value={validationSearchTerm} onChange={e => setValidationSearchTerm(e.target.value)} autoFocus />
            </div>
          )}

          <div className="options-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(110px, 1fr))', gap: '12px', maxHeight: '220px', overflowY: 'auto', padding: '15px', background: '#fcfcfc', border: '1px solid #f5f5f5', borderRadius: '15px' }}>
            {filteredOptions.map(opt => {
              const isSelected = step.isMulti ? validationData[step.field].includes(opt) : (step.isEnum ? (typeof validationData.type === 'number' ? CLOTHING_TYPES[validationData.type] === opt : validationData.type === opt) : validationData[step.field] === opt);
              const isAiPrediction = step.isEnum ? (CLOTHING_TYPES[originalPredictions.type] === opt) : (originalPredictions[step.field] === opt);
              return (
                <button key={opt} onClick={() => {
                  if (step.isMulti) {
                    const currentArray = validationData[step.field];
                    setValidationData({ ...validationData, [step.field]: currentArray.includes(opt) ? currentArray.filter(i => i !== opt) : [...currentArray, opt] });
                  } else {
                    setValidationData({ ...validationData, [step.field]: step.isEnum ? CLOTHING_TYPES.indexOf(opt) : opt });
                    if (step.isSearchable) setValidationSearchTerm('');
                  }
                }} style={{ padding: '10px 5px', fontSize: '0.6rem', fontFamily: 'JetBrains Mono, monospace', background: isSelected ? '#000' : '#fff', border: isSelected ? '1px solid #000' : (isAiPrediction ? '1px dashed #646cff' : '1px solid #eee'), color: isSelected ? '#fff' : (isAiPrediction ? '#646cff' : '#888'), borderRadius: '10px', cursor: 'pointer', textTransform: 'uppercase', letterSpacing: '1px', transition: 'all 0.2s ease', position: 'relative' }}>
                  {opt}
                  {isAiPrediction && !isSelected && <span style={{ position: 'absolute', top: '-5px', right: '5px', fontSize: '8px', color: '#646cff', fontWeight: 'bold' }}>AI</span>}
                </button>
              );
            })}
          </div>
        </div>

        <div className="modal-actions" style={{ display: 'flex', gap: '15px', marginTop: '20px' }}>
          {currentStep > 0 && <button className="close-link" onClick={() => { setValidationSearchTerm(''); setCurrentStep(currentStep - 1); }} style={{ flex: 1, padding: '12px' }}>BACK</button>}
          <button className="gen-btn" onClick={onConfirmStep} disabled={loading} style={{ flex: 2, padding: '12px' }}>{loading ? 'SAVING...' : (currentStep === 4 ? "COMPLETE & SAVE" : "CONTINUE")}</button>
        </div>
      </div>
    );
  };

  const onSaveAiOutfit = async () => {
    setLoading(true);
    try {
      const itemIds = aiData.selectedItems.map(i => i.id);
      await axios.post(`${API_BASE_URL}/outfits`, { userId, name: aiData.name, itemIds, isAiGenerated: true });
      setAiModal(false);
      setView('outfits');
      fetchOutfits();
    } catch (err) { alert("Save failed"); }
    finally { setLoading(false); }
  };

  const onSelectAiCandidate = (type, candidate) => {
    const newSelectedItems = aiData.selectedItems.map(item => {
      const clothItem = clothes.find(c => c.id === item.id);
      const candidateItem = clothes.find(c => c.id === candidate.id);
      if (clothItem && candidateItem && clothItem.type === candidateItem.type) return candidate;
      return item;
    });
    setAiData({ ...aiData, selectedItems: newSelectedItems });
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

  const onUpdateItem = async () => {
    setLoading(true);
    try {
      await axios.put(`${API_BASE_URL}/clothing/${editItemData.id}`, {
        ...editItemData,
        type: typeof editItemData.type === 'string' ? CLOTHING_TYPES.indexOf(editItemData.type.toUpperCase()) : editItemData.type,
        season: Array.isArray(editItemData.season) ? editItemData.season.join(', ') : editItemData.season,
        usage: Array.isArray(editItemData.usage) ? editItemData.usage.join(', ') : editItemData.usage,
        userId
      });
      setEditItemMode(false);
      setSelectedItem(null);
      fetchClothes();
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

  const onWearOutfit = async (outfitId) => {
    try {
      await axios.post(`${API_BASE_URL}/wear-events/outfit/${outfitId}`, {
        userId: userId
      });
      alert("Outfit recorded for today!");
      refresh(); 
    } catch (err) {
      const msg = err.response?.data || "Failed to record wear event.";
      console.error("Wear event error:", msg);
      alert(msg);
    }
  };

  const onSaveCustomOutfit = async () => {
    if (!customOutfitData.name || customOutfitData.itemIds.length === 0) {
      alert("Please provide a name and select at least one item.");
      return;
    }
    setLoading(true);
    try {
      await axios.post(`${API_BASE_URL}/outfits`, { 
        userId, 
        name: customOutfitData.name, 
        itemIds: customOutfitData.itemIds,
        isAiGenerated: false 
      });
      setCustomOutfitModal(false);
      setCustomOutfitData({ name: '', itemIds: [] });
      fetchOutfits();
    } catch (err) { alert("Save failed"); }
    finally { setLoading(false); }
  };

  return (
    <div className="desktop-wrapper">
      <aside className="side-nav">
        <div className="brand">W.</div>
        <div className="nav-links">
          <button className={`nav-btn ${view === 'clothes' ? 'active' : ''}`} onClick={() => setView('clothes')}>clothes</button>
          <button className={`nav-btn ${view === 'outfits' ? 'active' : ''}`} onClick={() => setView('outfits')}>outfits</button>
          <button className={`nav-btn ${view === 'stats' ? 'active' : ''}`} onClick={() => setView('stats')}>stats</button>
        </div>
        <button className="exit-circle" onClick={onLogout}></button>
      </aside>

      <main className="stage">
        <div className="centered-content">
          <h2 className="soft-title">
            {view === 'clothes' ? 'your wardrobe' : view === 'outfits' ? 'generated outfits' : 'wardrobe insights'}
          </h2>
          
          {view !== 'stats' && (
            <div className="weather-bar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', ...getWeatherStyles(), padding: '20px 30px', borderRadius: '20px', marginBottom: '30px', boxShadow: '0 10px 30px rgba(0,0,0,0.15)', color: getWeatherStyles().color || '#fff' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '30px' }}>
                <div style={{ textAlign: 'left', cursor: 'pointer' }} onClick={() => { setSearchTerm(''); setCityModal(true); }}>
                  <span className="robotic-text" style={{ fontSize: '0.65rem', opacity: 0.7, display: 'block', fontWeight: 'bold' }}>LOCATION (EDIT)</span>
                  <span style={{ fontSize: '1.4rem', fontWeight: '900', textTransform: 'uppercase', letterSpacing: '1px', borderBottom: '2px solid rgba(0,0,0,0.1)' }}>{city}</span>
                </div>
                {weatherInfo && (
                  <>
                    <div style={{ borderLeft: '2px solid rgba(0,0,0,0.1)', paddingLeft: '25px', textAlign: 'left' }}>
                      <span style={{ fontSize: '1.4rem', fontWeight: '900' }}>{weatherInfo.temperature.toFixed(0)}°C</span>
                      <span style={{ fontSize: '0.9rem', marginLeft: '10px', fontWeight: 'bold', opacity: 0.8, textTransform: 'uppercase' }}>{weatherInfo.condition}</span>
                    </div>
                    <div style={{ borderLeft: '2px solid rgba(0,0,0,0.1)', paddingLeft: '25px', textAlign: 'left' }}>
                      <span className="robotic-text" style={{ fontSize: '0.65rem', opacity: 0.7, display: 'block', fontWeight: 'bold' }}>RECOMMENDED SEASON</span>
                      <span style={{ fontSize: '1.1rem', fontWeight: '900', textTransform: 'uppercase' }}>{weatherInfo.seasonSuggestion}</span>
                    </div>
                  </>
                )}
              </div>
              <button className="gen-btn" onClick={() => onGenerate()} disabled={loading || clothes.length === 0} style={{ padding: '12px 30px', fontSize: '0.8rem', background: '#000', color: '#fff', border: 'none', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}>GENERATE TODAY'S OUTFIT</button>
            </div>
          )}

          {view === 'clothes' ? (
            <div className="wardrobe-container">
              <div className="upload-section">
                <div className="empty-state-card" onClick={() => fileInputRef.current.click()}>+ ADD NEW ITEM</div>
              </div>
              {CLOTHING_TYPES.map((typeName, typeIndex) => {
                const filtered = clothes.filter(i => i.type === typeIndex);
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
          ) : view === 'outfits' ? (
            <div className="outfits-list">
              <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '30px' }}>
                <button 
                  className="gen-btn"
                  onClick={() => setCustomOutfitModal(true)}
                  style={{ padding: '12px 30px', fontSize: '0.8rem', background: '#fff', color: '#000', border: '2px solid #000', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}
                >
                  + CREATE CUSTOM OUTFIT
                </button>
              </div>
              {outfits.map(o => (
                <div key={o.id} className="outfit-row">
                  <div className="outfit-info">
                    <div className="outfit-header-left">
                      <span className="outfit-name">{o.name}</span>
                      <button className="edit-mini-btn" onClick={() => { setEditData({ id: o.id, name: o.name, itemIds: o.items?.map(i => i.id) || [] }); setEditModal(true); }}>edit items</button>
                    </div>
                    <div className="outfit-actions">
                      <button onClick={() => onWearOutfit(o.id)} style={{ background: '#000', color: '#fff', border: 'none', padding: '6px 12px', borderRadius: '8px', fontSize: '0.6rem', fontWeight: 'bold', cursor: 'pointer' }}>WEAR TODAY</button>
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
          ) : (
            <StatsSection userId={userId} />
          )}
          <input type="file" ref={fileInputRef} onChange={(e) => { const file = e.target.files[0]; if (file) { setUploadData({ file, name: file.name.split('.')[0] }); setUploadModal(true); } }} accept=".jpg,.jpeg,.png,.webp" hidden />
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

      <Modal isOpen={!!selectedItem} onClose={() => { setSelectedItem(null); setEditItemMode(false); }} title={editItemMode ? `Editing ${selectedItem?.name}` : selectedItem?.name} size="medium">
        {selectedItem && (
          <div style={{ maxHeight: '80vh', overflowY: 'auto', padding: '10px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ textAlign: 'center', background: '#fcfcfc', borderRadius: '20px', padding: '15px', border: '1px solid #f5f5f5' }}>
              <img src={selectedItem.processedImageUrl} alt="" style={{ maxWidth: '100%', maxHeight: '350px', borderRadius: '15px', objectFit: 'contain' }} />
            </div>

            {editItemMode ? (
              <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>NAME</span>
                  <input className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.name} onChange={e => setEditItemData({...editItemData, name: e.target.value})} />
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>TYPE</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={typeof editItemData.type === 'number' ? CLOTHING_TYPES[editItemData.type] : editItemData.type} onChange={e => setEditItemData({...editItemData, type: CLOTHING_TYPES.indexOf(e.target.value)})}>
                    {CLOTHING_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>COLOR</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.color} onChange={e => setEditItemData({...editItemData, color: e.target.value})}>
                    {COLORS.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>GENDER</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.gender} onChange={e => setEditItemData({...editItemData, gender: e.target.value})}>
                    {GENDERS.map(g => <option key={g} value={g}>{g}</option>)}
                  </select>
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '10px' }}>SEASON (MULTI-SELECT)</span>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                    {SEASONS.map(s => {
                      const isSelected = editItemData.season.includes(s);
                      return (
                        <button 
                          key={s} 
                          onClick={() => {
                            const newSeasons = isSelected ? editItemData.season.filter(item => item !== s) : [...editItemData.season, s];
                            setEditItemData({...editItemData, season: newSeasons});
                          }}
                          style={{
                            padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid #000' : '1px solid #eee',
                            background: isSelected ? '#000' : '#fff', color: isSelected ? '#fff' : '#888', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                          }}
                        >{s.toUpperCase()}</button>
                      );
                    })}
                  </div>
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '10px' }}>USAGE / STYLE (MULTI-SELECT)</span>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                    {USAGES.map(u => {
                      const isSelected = editItemData.usage.includes(u);
                      return (
                        <button 
                          key={u} 
                          onClick={() => {
                            const newUsage = isSelected ? editItemData.usage.filter(item => item !== u) : [...editItemData.usage, u];
                            setEditItemData({...editItemData, usage: newUsage});
                          }}
                          style={{
                            padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid #000' : '1px solid #eee',
                            background: isSelected ? '#000' : '#fff', color: isSelected ? '#fff' : '#888', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                          }}
                        >{u.toUpperCase()}</button>
                      );
                    })}
                  </div>
                </div>
              </div>
            ) : (
              <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
                <div style={{ background: '#fcfcfc', padding: '12px', borderRadius: '12px', border: '1px solid #f5f5f5' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>TYPE</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{CLOTHING_TYPES[selectedItem.type] || selectedItem.type}</span>
                </div>
                <div style={{ background: '#fcfcfc', padding: '12px', borderRadius: '12px', border: '1px solid #f5f5f5' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>COLOR</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.color?.toUpperCase()}</span>
                </div>
                <div style={{ background: '#fcfcfc', padding: '12px', borderRadius: '12px', border: '1px solid #f5f5f5' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>GENDER</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.gender?.toUpperCase() || 'UNISEX'}</span>
                </div>
                <div style={{ background: '#fcfcfc', padding: '12px', borderRadius: '12px', border: '1px solid #f5f5f5' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>SEASON</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.season?.toUpperCase() || 'ANY'}</span>
                </div>
                <div style={{ background: '#fcfcfc', padding: '12px', borderRadius: '12px', border: '1px solid #f5f5f5', gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: '#ccc', display: 'block', marginBottom: '4px' }}>USAGE</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.usage?.toUpperCase() || 'CASUAL'}</span>
                </div>
              </div>
            )}

            <div className="modal-actions" style={{ display: 'flex', gap: '10px' }}>
              {editItemMode ? (
                <>
                  <button className="gen-btn" onClick={onUpdateItem} disabled={loading} style={{ flex: 2 }}>
                    {loading ? 'SAVING...' : 'SAVE CHANGES'}
                  </button>
                  <button className="close-link" onClick={() => setEditItemMode(false)} style={{ flex: 1 }}>
                    CANCEL
                  </button>
                </>
              ) : (
                <>
                  <button className="gen-btn" onClick={() => onGenerate(selectedItem)} disabled={loading} style={{ flex: 2 }}>
                    {loading ? 'GENERATING...' : 'GENERATE OUTFIT'}
                  </button>
                  <button className="close-link" onClick={() => { 
                    setEditItemData({
                      ...selectedItem, 
                      season: selectedItem.season ? selectedItem.season.split(',').map(s => s.trim()) : [],
                      usage: selectedItem.usage ? selectedItem.usage.split(',').map(u => u.trim()) : []
                    }); 
                    setEditItemMode(true); 
                  }} style={{ flex: 1 }}>
                    EDIT
                  </button>
                </>
              )}
            </div>
          </div>
        )}
      </Modal>

      <Modal isOpen={styleSelectionModal} onClose={() => setStyleSelectionModal(false)} title="SELECT OUTFIT STYLE" size="medium">
        <div style={{ padding: '10px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '15px' }}>
            {USAGES.map(style => (
              <button key={style} onClick={() => executeGeneration(style)} style={{ padding: '20px', background: '#fff', color: '#000', border: '1px solid #eee', borderRadius: '15px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                <span style={{ fontWeight: '900', fontSize: '0.9rem', letterSpacing: '1px' }}>{style.toUpperCase()}</span>
              </button>
            ))}
          </div>
        </div>
      </Modal>

      <Modal isOpen={aiModal} onClose={() => setAiModal(false)} title="AI OUTFIT SUGGESTION" size="large">
        {aiData && (
          <div style={{ maxHeight: '80vh', overflowY: 'auto', padding: '10px' }}>
            <input className="name-input" value={aiData.name} onChange={e => setAiData({...aiData, name: e.target.value})} style={{ width: '100%', fontSize: '24px', marginBottom: '20px' }} />
            <div className="clothes-grid">
              {aiData.selectedItems.map(item => (
                <div key={item.id} className="item-card"><img src={item.processedImageUrl} alt="" /></div>
              ))}
            </div>
            <div className="modal-actions" style={{ marginTop: '20px' }}>
              <Button label="CONFIRM & SAVE" onClick={onSaveAiOutfit} loading={loading} />
              <Button label="DISCARD" variant="secondary" onClick={() => setAiModal(false)} />
            </div>
          </div>
        )}
      </Modal>

      <Modal isOpen={validationModal} onClose={() => setValidationModal(false)} title="Verify AI Prediction" size="medium">
        {renderValidationStep()}
      </Modal>

      <Modal isOpen={cityModal} onClose={() => setCityModal(false)} title="SELECT LOCATION" size="small">
        <div style={{ padding: '10px' }}>
          <input className="name-input" placeholder="Type city..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} autoFocus />
          <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
            {citySuggestions.map((c, idx) => (
              <button key={idx} onClick={() => { handleCityChange(c.name); setCityModal(false); }} style={{ width: '100%', padding: '10px', textAlign: 'left', background: 'none', border: '1px solid #eee', marginBottom: '5px' }}>
                {c.name}, {c.country}
              </button>
            ))}
          </div>
        </div>
      </Modal>

      {/* CUSTOM OUTFIT MODAL */}
      <Modal isOpen={customOutfitModal} onClose={() => setCustomOutfitModal(false)} title="BUILD CUSTOM OUTFIT" size="large">
        <div className="edit-outfit-container">
          <input 
            className="name-input" 
            placeholder="Name your outfit (e.g. Casual Friday)..." 
            value={customOutfitData.name} 
            onChange={e => setCustomOutfitData({...customOutfitData, name: e.target.value})} 
          />
          
          <div style={{ display: 'flex', gap: '10px', overflowX: 'auto', paddingBottom: '10px', marginBottom: '20px' }}>
            {CLOTHING_TYPES.map((type, idx) => (
              <button 
                key={type}
                onClick={() => setCustomOutfitTab(idx)}
                style={{ 
                  padding: '8px 16px', 
                  borderRadius: '20px', 
                  border: 'none', 
                  background: customOutfitTab === idx ? '#000' : '#eee', 
                  color: customOutfitTab === idx ? '#fff' : '#888',
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

          <div className="edit-items-grid" style={{ maxHeight: '400px', overflowY: 'auto', padding: '10px', border: '1px solid #f0f0f0', borderRadius: '15px' }}>
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
              <div style={{ gridColumn: '1/-1', textAlign: 'center', padding: '40px', color: '#ccc', fontSize: '0.8rem' }}>
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
                  <div key={id} style={{ width: '30px', height: '30px', borderRadius: '50%', overflow: 'hidden', border: '1px solid #eee' }}>
                    <img src={item.processedImageUrl} style={{ width: '100%', height: '100%', objectFit: 'cover' }} alt=""/>
                  </div>
                )
              })}
            </div>
            <Button label="SAVE OUTFIT" onClick={onSaveCustomOutfit} loading={loading} />
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default DashboardPage;