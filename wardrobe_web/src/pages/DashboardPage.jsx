import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import Button from '../components/Button';
import Modal from '../components/Modal';

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
  
  const [uploadModal, setUploadModal] = useState(false);
  const [uploadData, setUploadData] = useState({ file: null, name: '' });
  
  // Validation Multi-step Modal
  const [validationModal, setValidationModal] = useState(false);
  const [validationData, setValidationData] = useState(null);
  const [originalPredictions, setOriginalPredictions] = useState(null);
  const [currentStep, setCurrentStep] = useState(0); // 0: Type, 1: Color, 2: Gender, 3: Season, 4: Usage
  const [validationSearchTerm, setValidationSearchTerm] = useState('');
  
  const [editModal, setEditModal] = useState(false);
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [] });

  const [aiModal, setAiModal] = useState(false);
  const [aiData, setAiData] = useState(null);
  const [city, setCity] = useState(localStorage.getItem('userCity') || 'Detecting...');
  const [cityModal, setCityModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [weatherInfo, setWeatherInfo] = useState(null);
  const [citySuggestions, setCitySuggestions] = useState([]);
  const [selectedStyle, setSelectedStyle] = useState('Casual');
  const [styleSelectionModal, setStyleSelectionModal] = useState(false);
  const [generationContext, setGenerationContext] = useState(null); // 'today' or 'item'

  const fileInputRef = useRef(null);
  const userId = user?.id || user?.Id;

  // Fetch dynamic city suggestions as user types in modal
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

    const timeoutId = setTimeout(searchCities, 400); // Debounce
    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  // 1. Improved location detection with fallback and persistence
  useEffect(() => {
    const detectLocation = async () => {
      const savedCity = localStorage.getItem('userCity');
      if (savedCity) {
        setCity(savedCity);
        return;
      }

      try {
        console.log("Attempting location detection via ipapi.co...");
        const res = await axios.get('https://ipapi.co/json/');
        if (res.data && res.data.city) {
          console.log("Detected city (ipapi):", res.data.city);
          setCity(res.data.city);
          localStorage.setItem('userCity', res.data.city);
        } else {
          throw new Error("Invalid response from ipapi");
        }
      } catch (e) {
        console.warn("ipapi.co failed, trying ip-api.com fallback...", e.message);
        try {
          const res = await axios.get('http://ip-api.com/json/');
          if (res.data && res.data.city) {
            console.log("Detected city (ip-api):", res.data.city);
            setCity(res.data.city);
            localStorage.setItem('userCity', res.data.city);
          } else {
            setCity('Bucharest');
          }
        } catch (e2) {
          console.error("All location services failed", e2);
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
        userId, startItemId: startItem.id, threshold: 0.5, city, style 
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
    } catch (err) { 
      console.error(err);
      alert("Save failed"); 
    }
    finally { setLoading(false); }
  };

  const renderValidationStep = () => {
    if (!validationData || !originalPredictions) return null;

    const steps = [
      { 
        label: 'TYPE', 
        value: validationData.type, 
        options: CLOTHING_TYPES,
        field: 'type',
        isEnum: true,
        original: originalPredictions.type
      },
      { 
        label: 'COLOR', 
        value: validationData.color, 
        options: COLORS, 
        field: 'color', 
        isSearchable: true,
        original: originalPredictions.color
      },
      { 
        label: 'GENDER', 
        value: validationData.gender, 
        options: GENDERS, 
        field: 'gender',
        original: originalPredictions.gender
      },
      { 
        label: 'SEASON', 
        value: validationData.season, 
        options: SEASONS, 
        field: 'season', 
        isMulti: true,
        original: originalPredictions.season
      },
      { 
        label: 'USAGE', 
        value: validationData.usage, 
        options: USAGES, 
        field: 'usage', 
        isMulti: true,
        original: originalPredictions.usage
      }
    ];

    const step = steps[currentStep];

    const getSortedOptions = () => {
      const { options, original, isEnum, label } = step;
      let originalLabel = null;
      
      if (isEnum) {
        // Handle TYPE enum mapping
        originalLabel = typeof original === 'number' ? CLOTHING_TYPES[original] : original;
      } else {
        originalLabel = original;
      }

      if (!originalLabel) return options;
      
      // Make sure we only put the prediction at the top IF it exists in the current options list
      const cleanOriginal = typeof originalLabel === 'string' ? originalLabel : String(originalLabel);
      const matchedOption = options.find(o => o.toLowerCase() === cleanOriginal.toLowerCase());
      
      if (!matchedOption) return options;
      
      const others = options.filter(o => o.toLowerCase() !== matchedOption.toLowerCase());
      return [matchedOption, ...others];
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
            style={{ 
              maxWidth: '100%', 
              maxHeight: '250px', 
              borderRadius: '20px', 
              border: '1px solid #f5f5f5', 
              padding: '10px', 
              background: '#fcfcfc',
              objectFit: 'contain' // Prevents stretching
            }} 
          />
        </div>
        
        <div className="step-indicator" style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '30px' }}>
          {steps.map((_, i) => (
            <div key={i} style={{ 
              width: '30px', 
              height: '3px', 
              background: i === currentStep ? '#000' : (i < currentStep ? '#eee' : '#f5f5f5'),
              borderRadius: '2px',
              transition: 'all 0.3s'
            }} />
          ))}
        </div>

        <div style={{ marginBottom: '30px', textAlign: 'left' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <span className="robotic-text" style={{ fontSize: '0.6rem', color: '#ccc' }}>
              STEP {currentStep + 1} OF 5: VERIFY {step.label}
            </span>
            {step.isMulti && (
              <span className="robotic-text" style={{ fontSize: '0.55rem', color: '#646cff' }}>
                (MULTI-SELECT)
              </span>
            )}
          </div>
          
          {step.isSearchable && (
            <div style={{ marginBottom: '15px' }}>
              <input 
                type="text" 
                placeholder={`Search ${step.label.toLowerCase()}...`}
                className="name-input"
                style={{ marginBottom: '10px', fontSize: '0.8rem', textAlign: 'left', padding: '10px 20px' }}
                value={validationSearchTerm}
                onChange={e => setValidationSearchTerm(e.target.value)}
                autoFocus
              />
            </div>
          )}

          <div className="options-grid" style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fill, minmax(110px, 1fr))', 
            gap: '12px',
            maxHeight: '220px',
            overflowY: 'auto',
            padding: '15px',
            background: '#fcfcfc',
            border: '1px solid #f5f5f5',
            borderRadius: '15px'
          }}>
            {filteredOptions.map(opt => {
              const isSelected = step.isMulti
                ? validationData[step.field].includes(opt)
                : (step.isEnum 
                    ? (typeof validationData.type === 'number' ? CLOTHING_TYPES[validationData.type] === opt : validationData.type === opt)
                    : validationData[step.field] === opt);
                
              const isAiPrediction = step.isEnum 
                ? (CLOTHING_TYPES[originalPredictions.type] === opt)
                : (originalPredictions[step.field] === opt);

              return (
                <button 
                  key={opt}
                  onClick={() => {
                    if (step.isMulti) {
                      const currentArray = validationData[step.field];
                      const newArray = currentArray.includes(opt)
                        ? currentArray.filter(i => i !== opt)
                        : [...currentArray, opt];
                      setValidationData({ ...validationData, [step.field]: newArray });
                    } else {
                      const newValue = step.isEnum ? CLOTHING_TYPES.indexOf(opt) : opt;
                      setValidationData({ ...validationData, [step.field]: newValue });
                      if (step.isSearchable) setValidationSearchTerm('');
                    }
                  }}
                  style={{
                    padding: '10px 5px',
                    fontSize: '0.6rem',
                    fontFamily: 'JetBrains Mono, monospace',
                    background: isSelected ? '#000' : '#fff',
                    border: isSelected ? '1px solid #000' : (isAiPrediction ? '1px dashed #646cff' : '1px solid #eee'),
                    color: isSelected ? '#fff' : (isAiPrediction ? '#646cff' : '#888'),
                    borderRadius: '10px',
                    cursor: 'pointer',
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    transition: 'all 0.2s ease',
                    boxShadow: isSelected ? '0 4px 10px rgba(0,0,0,0.1)' : 'none',
                    position: 'relative'
                  }}
                >
                  {opt}
                  {isAiPrediction && !isSelected && (
                    <span style={{ position: 'absolute', top: '-5px', right: '5px', fontSize: '8px', color: '#646cff', fontWeight: 'bold' }}>AI</span>
                  )}
                </button>
              );
            })}
            {filteredOptions.length === 0 && (
              <div style={{ gridColumn: '1/-1', textAlign: 'center', padding: '20px', color: '#ccc', fontSize: '0.7rem' }}>
                No results found
              </div>
            )}
          </div>
        </div>

        <div className="modal-actions" style={{ display: 'flex', gap: '15px', marginTop: '20px' }}>
          {currentStep > 0 && (
            <button 
              className="close-link" 
              onClick={() => {
                setValidationSearchTerm('');
                setCurrentStep(currentStep - 1);
              }}
              style={{ flex: 1, padding: '12px' }}
            >
              BACK
            </button>
          )}
          <button 
            className="gen-btn" 
            onClick={onConfirmStep} 
            disabled={loading}
            style={{ flex: 2, padding: '12px' }}
          >
            {loading ? 'SAVING...' : (currentStep === 4 ? "COMPLETE & SAVE" : "CONTINUE")}
          </button>
        </div>
      </div>
    );
  };

  const onSaveAiOutfit = async () => {
    setLoading(true);
    try {
      const itemIds = aiData.selectedItems.map(i => i.id);
      await axios.post(`${API_BASE_URL}/outfits`, { 
        userId, 
        name: aiData.name, 
        itemIds,
        isAiGenerated: true 
      });
      setAiModal(false);
      setView('outfits');
      fetchOutfits();
    } catch (err) { alert("Save failed"); }
    finally { setLoading(false); }
  };

  const onSelectAiCandidate = (type, candidate) => {
    const newSelectedItems = aiData.selectedItems.map(item => {
      // Find item of the same type (this logic is a bit simple as it relies on cloth object which we don't have fully in SimilarItemDto)
      // Actually, we can check the recommendation type
      const clothItem = clothes.find(c => c.id === item.id);
      const candidateItem = clothes.find(c => c.id === candidate.id);
      if (clothItem && candidateItem && clothItem.type === candidateItem.type) {
        return candidate;
      }
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
          
          {/* WEATHER BAR */}
          <div className="weather-bar" style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            ...getWeatherStyles(),
            padding: '20px 30px',
            borderRadius: '20px',
            marginBottom: '30px',
            boxShadow: '0 10px 30px rgba(0,0,0,0.15)',
            transition: 'all 0.5s ease',
            border: 'none',
            color: getWeatherStyles().color || '#fff'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '30px' }}>
              <div 
                style={{ textAlign: 'left', cursor: 'pointer' }} 
                onClick={() => {
                  setSearchTerm('');
                  setCityModal(true);
                }}
              >
                <span className="robotic-text" style={{ fontSize: '0.65rem', opacity: 0.7, display: 'block', fontWeight: 'bold' }}>LOCATION (EDIT)</span>
                <span style={{ 
                  fontSize: '1.4rem', 
                  fontWeight: '900',
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  borderBottom: '2px solid rgba(0,0,0,0.1)'
                }}>
                  {city}
                </span>
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
            <button 
              className="gen-btn" 
              onClick={() => {
                setGenerationContext('today');
                setStyleSelectionModal(true);
              }}
              disabled={loading || clothes.length === 0}
              style={{ 
                padding: '12px 30px', 
                fontSize: '0.8rem', 
                background: '#000', 
                color: '#fff',
                border: 'none',
                borderRadius: '12px',
                fontWeight: 'bold',
                boxShadow: '0 4px 15px rgba(0,0,0,0.3)',
                cursor: 'pointer',
                transition: 'transform 0.2s'
              }}
              onMouseEnter={(e) => e.target.style.transform = 'scale(1.05)'}
              onMouseLeave={(e) => e.target.style.transform = 'scale(1)'}
            >
              GENERATE TODAY'S OUTFIT
            </button>
          </div>

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
          <input 
            type="file" 
            ref={fileInputRef} 
            onChange={(e) => {
              const file = e.target.files[0];
              if (file) { setUploadData({ file, name: file.name.split('.')[0] }); setUploadModal(true); }
            }} 
            accept=".jpg,.jpeg,.png,.webp" 
            hidden 
          />
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

      <Modal isOpen={!!selectedItem} onClose={() => setSelectedItem(null)} title={selectedItem?.name} size="medium">
        {selectedItem && (
          <div style={{ 
            maxHeight: '80vh', 
            overflowY: 'auto', 
            padding: '10px',
            display: 'flex',
            flexDirection: 'column',
            gap: '20px'
          }}>
            <div style={{ textAlign: 'center', background: '#fcfcfc', borderRadius: '20px', padding: '15px', border: '1px solid #f5f5f5' }}>
              <img 
                src={selectedItem.processedImageUrl} 
                alt="" 
                style={{ 
                  maxWidth: '100%', 
                  maxHeight: '350px', 
                  borderRadius: '15px',
                  objectFit: 'contain'
                }} 
              />
            </div>

            <div className="inspect-grid" style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(2, 1fr)', 
              gap: '10px' 
            }}>
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

            <div className="modal-actions" style={{ display: 'flex', gap: '10px' }}>
              <button className="gen-btn" onClick={() => onGenerate(selectedItem)} disabled={loading} style={{ flex: 2 }}>
                {loading ? 'GENERATING...' : 'GENERATE OUTFIT'}
              </button>
              <button className="close-link" onClick={() => setSelectedItem(null)} style={{ flex: 1 }}>
                CLOSE
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* STYLE SELECTION MODAL */}
      <Modal isOpen={styleSelectionModal} onClose={() => setStyleSelectionModal(false)} title="SELECT OUTFIT STYLE" size="medium">
        <div style={{ padding: '10px' }}>
          <p style={{ fontSize: '0.8rem', color: '#666', marginBottom: '20px', textAlign: 'center' }}>
            Choose the occasion for your outfit. Our AI will filter items based on your style and today's weather.
          </p>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(2, 1fr)', 
            gap: '15px' 
          }}>
            {USAGES.map(style => (
              <button
                key={style}
                onClick={() => executeGeneration(style)}
                style={{
                  padding: '20px',
                  background: '#fff',
                  color: '#000',
                  border: '1px solid #eee',
                  borderRadius: '15px',
                  cursor: 'pointer',
                  transition: 'all 0.2s',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  gap: '10px',
                  boxShadow: '0 2px 8px rgba(0,0,0,0.02)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.background = '#000';
                  e.currentTarget.style.color = '#fff';
                  e.currentTarget.style.transform = 'translateY(-3px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background = '#fff';
                  e.currentTarget.style.color = '#000';
                  e.currentTarget.style.transform = 'translateY(0)';
                }}
              >
                <span style={{ fontWeight: '900', fontSize: '0.9rem', letterSpacing: '1px' }}>{style.toUpperCase()}</span>
              </button>
            ))}
          </div>
        </div>
      </Modal>

      <Modal isOpen={aiModal} onClose={() => setAiModal(false)} title="AI OUTFIT SUGGESTION" size="large">
        {aiData && (
          <div style={{ maxHeight: '80vh', overflowY: 'auto', padding: '10px' }}>
            
            {/* OUTFIT NAME SECTION - INTUITIVE EDITING */}
            <div style={{ marginBottom: '30px', borderBottom: '1px solid rgba(255,255,255,0.1)', paddingBottom: '20px' }}>
              <span className="robotic-text" style={{ fontSize: '10px', opacity: 0.5, letterSpacing: '2px', display: 'block', marginBottom: '10px' }}>
                OUTFIT CONFIGURATION ✎ <small style={{ opacity: 0.5, fontWeight: 'normal' }}>(CLICK NAME TO EDIT)</small>
              </span>
              <div className="editable-title-container" style={{ position: 'relative' }}>
                <input 
                  className="name-input" 
                  style={{ 
                    width: '100%', 
                    fontSize: '28px', 
                    background: 'rgba(255,255,255,0.03)', 
                    border: '1px solid rgba(255,255,255,0.1)', 
                    padding: '10px 15px',
                    borderRadius: '8px',
                    outline: 'none',
                    color: '#fff',
                    fontWeight: 'bold',
                    transition: 'all 0.3s'
                  }}
                  value={aiData.name} 
                  onChange={e => setAiData({...aiData, name: e.target.value})} 
                  onFocus={(e) => e.target.style.borderColor = '#646cff'}
                  onBlur={(e) => e.target.style.borderColor = 'rgba(255,255,255,0.1)'}
                />
              </div>
            </div>

            {/* THE LOOK - RESULT SECTION */}
            <div className="category-section" style={{ marginBottom: '40px' }}>
              <h3 className="category-title" style={{ fontSize: '14px', marginBottom: '20px' }}>THE LOOK</h3>
              <div style={{ 
                display: 'grid', 
                gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))', 
                gap: '20px',
                background: 'rgba(255,255,255,0.02)',
                padding: '25px',
                borderRadius: '15px',
                border: '1px solid rgba(255,255,255,0.05)'
              }}>
                {aiData.selectedItems.map(item => (
                  <div key={item.id} className="item-card" style={{ cursor: 'default', height: '160px' }}>
                    <img src={item.processedImageUrl} alt="" />
                    <span className="item-name-tag" style={{ fontSize: '10px' }}>
                      {item.name}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {/* REFINEMENTS - ALTERNATIVES SECTION - ENLARGED IMAGES */}
            <div className="category-section">
              <h3 className="category-title" style={{ fontSize: '14px', marginBottom: '20px', opacity: 0.6 }}>WE MAY RECOMMEND YOU THIS:</h3>
              
              <div style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                {aiData.recommendationsPerType.map(rec => (
                  <div key={rec.type} style={{ 
                    display: 'flex', 
                    alignItems: 'center', 
                    gap: '40px', 
                    padding: '15px',
                    borderBottom: '1px solid rgba(255,255,255,0.03)'
                  }}>
                    <div style={{ width: '120px', flexShrink: 0 }}>
                      <span className="robotic-text" style={{ fontSize: '12px', color: '#646cff', fontWeight: 'bold' }}>
                        {CLOTHING_TYPES[rec.type] || rec.type}
                      </span>
                    </div>
                    
                    <div style={{ display: 'flex', gap: '20px' }}>
                      {rec.topCandidates.map(cand => {
                        const isActive = aiData.selectedItems.some(si => si.id === cand.id);
                        return (
                          <div 
                            key={cand.id} 
                            onClick={() => onSelectAiCandidate(rec.type, cand)}
                            className={`item-card ${isActive ? 'active' : ''}`}
                            style={{ 
                              width: '100px', 
                              height: '130px', 
                              opacity: isActive ? 1 : 0.45,
                              border: isActive ? '2px solid #646cff' : '1px solid rgba(255,255,255,0.1)',
                              transform: isActive ? 'scale(1.1)' : 'none',
                              transition: 'all 0.3s ease',
                              cursor: 'pointer'
                            }}
                          >
                            <img src={cand.processedImageUrl} alt="" style={{ height: '95px' }} />
                            <div style={{ 
                              position: 'absolute', 
                              top: '-8px', 
                              right: '-8px', 
                              background: '#646cff', 
                              borderRadius: '50%', 
                              width: '20px', 
                              height: '20px', 
                              display: isActive ? 'flex' : 'none',
                              alignItems: 'center',
                              justifyContent: 'center',
                              fontSize: '12px',
                              boxShadow: '0 0 10px rgba(100, 108, 255, 0.5)',
                              zIndex: 2
                            }}>✓</div>
                            <span className="item-name-tag" style={{ fontSize: '9px', padding: '4px' }}>
                              {(cand.similarityScore * 100).toFixed(0)}%
                            </span>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* ACTION FOOTER */}
            <div className="modal-actions" style={{ marginTop: '40px', paddingTop: '20px', borderTop: '1px solid rgba(255,255,255,0.1)' }}>
              <Button label="CONFIRM & SAVE" onClick={onSaveAiOutfit} loading={loading} />
              <Button label="DISCARD" variant="secondary" onClick={() => setAiModal(false)} />
            </div>

          </div>
        )}
      </Modal>

      <Modal isOpen={validationModal} onClose={() => setValidationModal(false)} title="Verify AI Prediction" size="medium">
        {renderValidationStep()}
      </Modal>

      {/* CITY SELECTION MODAL */}
      <Modal isOpen={cityModal} onClose={() => setCityModal(false)} title="SELECT LOCATION" size="small">
        <div style={{ padding: '10px' }}>
          <input 
            className="name-input"
            placeholder="Type city name (e.g. Iasi)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            autoFocus
            style={{ marginBottom: '20px', textAlign: 'left', padding: '12px 20px' }}
          />
          
          <div style={{ 
            maxHeight: '300px', 
            overflowY: 'auto', 
            display: 'flex', 
            flexDirection: 'column', 
            gap: '8px' 
          }}>
            {citySuggestions.map((c, idx) => (
              <button
                key={`${c.name}-${idx}`}
                onClick={() => {
                  handleCityChange(c.name);
                  setCityModal(false);
                }}
                style={{
                  padding: '15px',
                  background: 'rgba(0,0,0,0.03)',
                  border: '1px solid rgba(0,0,0,0.05)',
                  borderRadius: '12px',
                  textAlign: 'left',
                  cursor: 'pointer',
                  transition: 'all 0.2s',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}
                onMouseEnter={(e) => e.target.style.background = 'rgba(100, 108, 255, 0.1)'}
                onMouseLeave={(e) => e.target.style.background = 'rgba(0,0,0,0.03)'}
              >
                <span style={{ fontWeight: 'bold', color: '#000' }}>{c.name}</span>
                <span style={{ fontSize: '0.7rem', color: '#666' }}>{c.state ? `${c.state}, ` : ''}{c.country}</span>
              </button>
            ))}
            {searchTerm.length >= 3 && citySuggestions.length === 0 && (
              <div style={{ textAlign: 'center', padding: '20px', color: '#999', fontSize: '0.8rem' }}>
                Searching for results...
              </div>
            )}
            {searchTerm.length < 3 && (
              <div style={{ textAlign: 'center', padding: '20px', color: '#999', fontSize: '0.8rem' }}>
                Enter at least 3 characters
              </div>
            )}
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default DashboardPage;
