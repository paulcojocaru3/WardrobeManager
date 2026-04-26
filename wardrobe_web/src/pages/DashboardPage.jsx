import React, { useCallback, useMemo, useState, useEffect, useRef } from 'react';
import Button from '../components/Button';
import Modal from '../components/Modal';
import OutfitEditingModal from '../components/OutfitEditingModal';
import StatsSection from '../components/StatsSection';
import WeatherBar from '../components/WeatherBar';
import { clothingApi, geoApi, outfitsApi, plannerEventsApi, statsApi } from '../services/wardrobeApi';
import { COLORS, CLOTHING_TYPES, GENDERS, SEASONS, USAGES } from '../constants/wardrobe';
import { getErrorMessage } from '../utils/errors';
import { toCsv, toStringArray, toTypeIndex } from '../utils/wardrobeTransforms';
import { useTheme } from '../contexts/ThemeContext';

const DashboardPage = ({ user, onLogout }) => {
  const { isDarkMode, toggleTheme } = useTheme();
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [plannerEvents, setPlannerEvents] = useState([]);
  const [usageRate, setUsageRate] = useState(0);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('dashboard');
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

  const [planModal, setPlanModal] = useState(false);
  const [planData, setPlanData] = useState({ outfitId: null, plannerEventId: '', selectedDayIndex: null, moment: '' });

  // Helper to get days for an event
  const getEventDays = useCallback((event) => {
    if (!event || !event.startDate || !event.endDate) return [];
    const days = [];
    const start = new Date(event.startDate);
    const end = new Date(event.endDate);
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      days.push({
        index: days.length,
        date: new Date(d),
        label: `Day ${days.length + 1} - ${new Date(d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', weekday: 'short' })}`
      });
    }
    return days;
  }, []);

  const currentEventDays = useMemo(() => {
    const event = plannerEvents.find(e => e.id === planData.plannerEventId);
    return event ? getEventDays(event) : [];
  }, [planData.plannerEventId, plannerEvents, getEventDays]);

  const todaysEvents = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    return plannerEvents.filter(event => {
      const start = new Date(event.startDate);
      start.setHours(0, 0, 0, 0);
      const end = new Date(event.endDate);
      end.setHours(23, 59, 59, 999);
      return today >= start && today <= end;
    }).map(event => {
      const itinerary = event.itineraries?.find(it => {
        const itDate = new Date(it.date);
        itDate.setHours(0, 0, 0, 0);
        return itDate.getTime() === today.getTime();
      });
      return { ...event, todayItinerary: itinerary };
    });
  }, [plannerEvents]);

  const [editItineraryModal, setEditItineraryModal] = useState(false);
const [editItineraryData, setEditItineraryData] = useState({
    plannerEventId: '',
    itineraryId: '',
    outfitId: '',
    date: '',
    moment: ''
  });

  const [createEventModal, setCreateEventModal] = useState(false);
  const [createEventData, setCreateEventData] = useState({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '' });
  const [eventLocationSearch, setEventLocationSearch] = useState('');
  const [eventLocationSuggestions, setEventLocationSuggestions] = useState([]);

  const [generatingModal, setGeneratingModal] = useState(false);
  const [generatingProgress, setGeneratingProgress] = useState(null);

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

  // Event weather forecasts
  const [eventForecasts, setEventForecasts] = useState({});

  // Event creation wizard state
  const [wizardStep, setWizardStep] = useState(0);
  const [wizardPreview, setWizardPreview] = useState(null);
  const [wizardLoading, setWizardLoading] = useState(false);

  // Premium Planner State
  const [selectedPlannerEvent, setSelectedPlannerEvent] = useState(null);
  const [selectedDayIndex, setSelectedDayIndex] = useState(null);
  const [archivedPlannerEvents, setArchivedPlannerEvents] = useState([]);
  const [plannerEventTab, setPlannerEventTab] = useState('active'); // 'active' or 'archived'
  
  // Outfit Editing Modal State
  const [outfitEditingModal, setOutfitEditingModal] = useState(false);
  const [outfitEditingData, setOutfitEditingData] = useState({
    plannerEventId: null,
    itineraryId: null,
    outfitId: null,
    date: null,
    moment: null,
    dayIndex: null,
  });

  // Planner derived selectors
  const plannerDays = useMemo(() => {
    if (!selectedPlannerEvent) return [];
    const days = [];
    const start = new Date(selectedPlannerEvent.startDate);
    const end = new Date(selectedPlannerEvent.endDate);
    const forecasts = eventForecasts[selectedPlannerEvent.id] || [];
    
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      const dayDate = new Date(d);
      const itinerary = selectedPlannerEvent.itineraries?.find(it => 
        new Date(it.date).toDateString() === dayDate.toDateString()
      );
      
      // Match forecast by date
      const forecast = forecasts.find(f => {
        const fDate = new Date(f.date);
        return fDate.toDateString() === dayDate.toDateString();
      });
      
      days.push({
        date: new Date(dayDate),
        dayNumber: days.length + 1,
        itinerary: itinerary || null,
        weather: forecast || null
      });
    }
    return days;
  }, [selectedPlannerEvent, eventForecasts]);

  const selectedDayItinerary = useMemo(() => {
    if (selectedDayIndex === null || !plannerDays[selectedDayIndex]) return null;
    return plannerDays[selectedDayIndex].itinerary;
  }, [selectedDayIndex, plannerDays]);

  const fileInputRef = useRef(null);
  const handleApiAlert = (error, fallback) => {
    alert(getErrorMessage(error, fallback));
  };

  const userId = user?.id || user?.Id;
  const userDisplayName = user?.username || user?.Username || user?.email || user?.Email || 'wardrobe user';
  const userEmail = user?.email || user?.Email || 'no email';
  const userCreatedAt = user?.createdAt || user?.CreatedAt;
  const memberSince = userCreatedAt ? new Date(userCreatedAt).toLocaleDateString() : 'recently';
  const userInitials = useMemo(
    () => userDisplayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('') || 'U',
    [userDisplayName]
  );

  const aiOutfitCount = useMemo(() => outfits.filter((outfit) => outfit.isAiGenerated).length, [outfits]);
  const customOutfitCount = useMemo(() => Math.max(outfits.length - aiOutfitCount, 0), [outfits.length, aiOutfitCount]);
  const weatherSummary = useMemo(
    () => (weatherInfo ? `${Math.round(weatherInfo.temperature)}°C • ${weatherInfo.condition}` : 'updating...'),
    [weatherInfo]
  );
  const trackedStyles = useMemo(
    () => new Set(
      clothes.flatMap((item) => (item.usage || '')
        .split(',')
        .map((entry) => entry.trim().toLowerCase())
        .filter(Boolean))
    ).size,
    [clothes]
  );

  useEffect(() => {
    const detectLocation = async () => {
      const savedCity = localStorage.getItem('userCity');
      if (savedCity) {
        setCity(savedCity);
        return;
      }

      try {
        const res = await geoApi.detectPrimary();
        if (res.data && res.data.city) {
          setCity(res.data.city);
          localStorage.setItem('userCity', res.data.city);
        } else {
          throw new Error("Invalid response from ipapi");
        }
      } catch {
        try {
          const res = await geoApi.detectFallback();
          if (res.data && res.data.city) {
            setCity(res.data.city);
            localStorage.setItem('userCity', res.data.city);
          } else {
            setCity('Bucharest');
          }
        } catch {
          setCity('Bucharest');
        }
      }
    };
    detectLocation();
  }, []);

  const handleCityChange = useCallback((newCity) => {
    setCity(newCity);
    localStorage.setItem('userCity', newCity);
  }, []);

const fetchWeather = useCallback(async () => {
    if (city === 'Detecting...') return;

    try {
      const res = await outfitsApi.getWeather(city);
      setWeatherInfo(res.data);
    } catch (e) {
      console.error('Weather error:', e);
    }
  }, [city]);

  const fetchEventForecast = useCallback(async (event) => {
    if (!event?.location) return;
    
    try {
      const res = await outfitsApi.getForecast(event.location, event.startDate);
      if (res.data?.forecasts) {
        setEventForecasts(prev => ({
          ...prev,
          [event.id]: res.data.forecasts
        }));
      }
    } catch (e) {
      console.error('Forecast error:', e);
    }
  }, []);

  const fetchClothes = useCallback(async () => {
    if (!userId) {
      return;
    }

    try {
      const res = await clothingApi.getByUser(userId);
      setClothes(Array.isArray(res.data) ? res.data : []);
    } catch (e) {
      console.error('Clothes error:', e);
    }
  }, [userId]);

  const fetchOutfits = useCallback(async () => {
    if (!userId) {
      return;
    }

    try {
      const res = await outfitsApi.getByUser(userId);
      setOutfits(Array.isArray(res.data) ? res.data : []);
    } catch (e) {
      console.error('Outfits error:', e);
    }
  }, [userId]);

  const fetchPlannerEvents = useCallback(async () => {
    if (!userId) {
      return;
    }

    try {
      const res = await plannerEventsApi.getByUser(userId);
      setPlannerEvents(Array.isArray(res.data) ? res.data : []);
    } catch (e) {
      console.error('Planner events error:', e);
    }
  }, [userId]);

  const fetchArchivedPlannerEvents = useCallback(async () => {
    if (!userId) {
      return;
    }

    try {
      const res = await plannerEventsApi.getArchivedByUser(userId);
      setArchivedPlannerEvents(Array.isArray(res.data) ? res.data : []);
    } catch (e) {
      console.error('Archived planner events error:', e);
    }
  }, [userId]);

  const fetchUsageRate = useCallback(async () => {
    if (!userId) return;
    try {
      const res = await statsApi.getWearStats(userId, { range: '7d' });
      setUsageRate(res.data?.utilizationRate || 0);
    } catch (e) {
      console.error('Usage rate error:', e);
    }
  }, [userId]);

  const refresh = useCallback(() => {
    if (!userId) return;

    fetchClothes();
    fetchOutfits();
    fetchPlannerEvents();
    fetchArchivedPlannerEvents();
    fetchWeather();
    fetchUsageRate();
  }, [fetchClothes, fetchOutfits, fetchPlannerEvents, fetchArchivedPlannerEvents, fetchWeather, fetchUsageRate, userId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    if (!userId) {
      return;
    }

    const timeoutId = setTimeout(async () => {
      if (!searchTerm || searchTerm.length < 3) {
        setCitySuggestions([]);
        return;
      }

      try {
        const res = await outfitsApi.searchCities(searchTerm);
        setCitySuggestions(res.data);
      } catch (error) {
        console.error('City search error', error);
      }
    }, 400);

return () => clearTimeout(timeoutId);
  }, [searchTerm, userId]);

  // Event location search
  useEffect(() => {
    if (!eventLocationSearch || eventLocationSearch.length < 2) {
      setEventLocationSuggestions([]);
      return;
    }

    const timeoutId = setTimeout(async () => {
      try {
        const res = await outfitsApi.searchCities(eventLocationSearch);
        setEventLocationSuggestions(res.data || []);
      } catch (error) {
        console.error('Event location search error', error);
      }
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [eventLocationSearch]);

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
      const { data } = await outfitsApi.generateAi({
        userId, 
        startItemId: startItem.id, 
        threshold: 0.5, 
        city, 
        style,
        season: weatherInfo?.seasonSuggestion 
      });
      setAiData(data);
      setAiModal(true);
    } catch (err) {
      handleApiAlert(err, 'Generation failed');
    }
    finally { setLoading(false); setSelectedItem(null); }
  };

  const onUpload = async () => {
    setLoading(true);
    const fd = new FormData();
    fd.append('File', uploadData.file);
    fd.append('UserId', userId);
    fd.append('Name', uploadData.name);
    try {
      const res = await clothingApi.process(fd);
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
    } catch (err) {
      handleApiAlert(err, 'Processing failed');
    }
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
      const payloadType = toTypeIndex(validationData.type);
      const payload = {
        userId,
        name: validationData.name,
        type: payloadType,
        color: validationData.color,
        gender: validationData.gender,
        season: toCsv(validationData.season),
        usage: toCsv(validationData.usage),
        processedImageB64: validationData.processedImageB64,
        embedding: validationData.embedding
      };
      
      if (payload.type === -1 && typeof validationData.type === 'number') {
          payload.type = validationData.type;
      }

      await clothingApi.add(payload);
      setValidationModal(false);
      fetchClothes();
    } catch (err) {
      handleApiAlert(err, 'Save failed');
    }
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
            <div key={i} style={{ width: '30px', height: '3px', background: i === currentStep ? 'var(--accent)' : (i < currentStep ? 'var(--border-muted)' : 'var(--border-subtle)'), borderRadius: '2px', transition: 'all 0.3s' }} />
          ))}
        </div>

        <div style={{ marginBottom: '30px', textAlign: 'left' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <span className="robotic-text" style={{ fontSize: '0.6rem', color: 'var(--fg-faint)' }}>STEP {currentStep + 1} OF 5: VERIFY {step.label}</span>
          </div>
          
          {step.isSearchable && (
            <div style={{ marginBottom: '15px' }}>
              <input type="text" placeholder={`Search...`} className="name-input" style={{ marginBottom: '10px', fontSize: '0.8rem', textAlign: 'left', padding: '10px 20px' }} value={validationSearchTerm} onChange={e => setValidationSearchTerm(e.target.value)} autoFocus />
            </div>
          )}

          <div className="options-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(110px, 1fr))', gap: '12px', maxHeight: '220px', overflowY: 'auto', padding: '15px', background: 'var(--bg-subtle)', border: '1px solid var(--border-subtle)', borderRadius: '15px' }}>
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
                }} style={{ padding: '10px 5px', fontSize: '0.6rem', fontFamily: 'JetBrains Mono, monospace', background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', border: isSelected ? '1px solid var(--accent)' : (isAiPrediction ? '1px dashed #646cff' : '1px solid var(--border-muted)'), color: isSelected ? 'var(--accent-fg)' : (isAiPrediction ? '#646cff' : 'var(--fg-muted)'), borderRadius: '10px', cursor: 'pointer', textTransform: 'uppercase', letterSpacing: '1px', transition: 'all 0.2s ease', position: 'relative' }}>
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
      await outfitsApi.create({ userId, name: aiData.name, itemIds, isAiGenerated: true });
      setAiModal(false);
      setView('outfits');
      fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Save failed');
    }
    finally { setLoading(false); }
  };

  const onEditSave = async () => {
    setLoading(true);
    try {
      await outfitsApi.update(editData.id, editData);
      setEditModal(false);
      fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Update failed');
    }
    finally { setLoading(false); }
  };

  const onUpdateItem = async () => {
    setLoading(true);
    try {
      await clothingApi.update(editItemData.id, {
        ...editItemData,
        type: toTypeIndex(editItemData.type),
        season: toCsv(editItemData.season),
        usage: toCsv(editItemData.usage),
        userId
      });
      setEditItemMode(false);
      setSelectedItem(null);
      fetchClothes();
    } catch (err) {
      handleApiAlert(err, 'Update failed');
    }
    finally { setLoading(false); }
  };

  const onDelete = async (type, id) => {
    try {
      if (type === 'cloth') {
        await clothingApi.remove(id);
      } else {
        await outfitsApi.remove(id);
      }

      type === 'cloth' ? fetchClothes() : fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Delete failed');
    }
  };

  const onWearOutfit = async (outfitId) => {
    try {
      await outfitsApi.recordWear(outfitId, { userId });
      alert("Outfit recorded for today!");
      refresh(); 
    } catch (err) {
      const message = getErrorMessage(err, 'Failed to record wear event.');
      console.error('Wear event error:', message);
      alert(message);
    }
  };

  const onSaveCustomOutfit = async () => {
    if (!customOutfitData.name || customOutfitData.itemIds.length === 0) {
      alert("Please provide a name and select at least one item.");
      return;
    }
    setLoading(true);
    try {
      await outfitsApi.create({
        userId, 
        name: customOutfitData.name, 
        itemIds: customOutfitData.itemIds,
        isAiGenerated: false 
      });
      setCustomOutfitModal(false);
      setCustomOutfitData({ name: '', itemIds: [] });
      fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Save failed');
    }
    finally { setLoading(false); }
  };

  const onCreatePlannerEvent = async () => {
    if (!createEventData.name || !createEventData.location || !createEventData.startDate || !createEventData.endDate) {
      alert("Please fill all fields.");
      return;
    }
    
    setLoading(true);
    try {
      await plannerEventsApi.create({
        userId,
        ...createEventData,
        startDate: new Date(createEventData.startDate).toISOString(),
        endDate: new Date(createEventData.endDate).toISOString()
      });
      
      // Reset wizard state
      setWizardStep(0);
      setWizardPreview(null);
      setCreateEventModal(false);
      setCreateEventData({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '' });
      fetchPlannerEvents();
    } catch (err) {
      handleApiAlert(err, 'Create event failed');
      console.error('Create event error:', err);
    }
    finally { setLoading(false); }
  };

  const onPreviewEvent = async () => {
    if (!createEventData.location || !createEventData.startDate || !createEventData.endDate) {
      alert("Please fill location and dates first.");
      return;
    }
    
    setWizardLoading(true);
    try {
      // Get weather forecast for the location
      const res = await outfitsApi.getForecast(createEventData.location, createEventData.startDate);
      const forecasts = res.data?.forecasts || [];
      
      // Calculate days
      const startDate = new Date(createEventData.startDate);
      const endDate = new Date(createEventData.endDate);
      const days = [];
      
      for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
        const dayDate = new Date(d);
        const forecast = forecasts.find(f => {
          const fDate = new Date(f.date);
          return fDate.toDateString() === dayDate.toDateString();
        });
        
        days.push({
          date: new Date(dayDate),
          dayNumber: days.length + 1,
          weather: forecast || { temperature: 20, condition: 'Unknown' }
        });
      }
      
      setWizardPreview({ days, location: createEventData.location });
      setWizardStep(1);
    } catch (err) {
      console.error('Preview error:', err);
      // Still allow moving to preview even if weather fails
      const startDate = new Date(createEventData.startDate);
      const endDate = new Date(createEventData.endDate);
      const days = [];
      
      for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
        days.push({
          date: new Date(d),
          dayNumber: days.length + 1,
          weather: { temperature: 20, condition: 'Weather unavailable' }
        });
      }
      
      setWizardPreview({ days, location: createEventData.location });
      setWizardStep(1);
    } finally {
      setWizardLoading(false);
    }
  };

  const onDeletePlannerEvent = async (plannerEventId) => {
    try {
      await plannerEventsApi.remove(userId, plannerEventId);
      fetchPlannerEvents();
    } catch (err) {
      handleApiAlert(err, 'Delete event failed');
    }
  };

  const onArchiveEvent = async (plannerEventId) => {
    setLoading(true);
    try {
      await plannerEventsApi.archiveEvent(plannerEventId, { userId });
      setSelectedPlannerEvent(null);
      fetchPlannerEvents();
      fetchArchivedPlannerEvents();
      setPlannerEventTab('active');
    } catch (err) {
      handleApiAlert(err, 'Archive event failed');
    } finally {
      setLoading(false);
    }
  };

  const onPlanOutfit = async () => {
    if (!planData.plannerEventId || planData.selectedDayIndex === null || !planData.moment) {
      alert("Please fill all fields.");
      return;
    }
    const event = plannerEvents.find(e => e.id === planData.plannerEventId);
    if (!event) return;
    const dayDate = new Date(event.startDate);
    dayDate.setDate(dayDate.getDate() + planData.selectedDayIndex);
    
    setLoading(true);
    try {
      await plannerEventsApi.addItinerary(planData.plannerEventId, {
        userId,
        outfitId: planData.outfitId,
        date: dayDate.toISOString(),
        moment: planData.moment
      });
      setPlanModal(false);
      setPlanData({ outfitId: null, plannerEventId: '', selectedDayIndex: null, moment: '' });
      fetchPlannerEvents();
    } catch (err) {
      handleApiAlert(err, 'Plan failed');
    }
    finally { setLoading(false); }
  };

const onDeleteItinerary = async (plannerEventId, itineraryId) => {
    try {
      await plannerEventsApi.removeItinerary(userId, plannerEventId, itineraryId);
      
      // Update local state immediately
      if (selectedPlannerEvent && selectedPlannerEvent.id === plannerEventId) {
        const updatedEvent = {
          ...selectedPlannerEvent,
          itineraries: selectedPlannerEvent.itineraries?.filter(it => it.id !== itineraryId) || []
        };
        setSelectedPlannerEvent(updatedEvent);
        
        // Also update in plannerEvents array
        setPlannerEvents(prev => prev.map(ev => 
          ev.id === plannerEventId ? updatedEvent : ev
        ));
      }
    } catch (err) {
      handleApiAlert(err, 'Delete itinerary failed');
    }
  };

const openOutfitEditingModal = (plannerEventId, itinerary, dayInfo, dayIndex) => {
    setOutfitEditingData({
      plannerEventId,
      itineraryId: itinerary.id,
      outfitId: itinerary.outfitId,
      date: itinerary.date,
      moment: itinerary.moment,
      dayIndex,
    });
    setOutfitEditingModal(true);
  };

const onSaveOutfitEdit = async (saveData) => {
    const { outfitId, itemIds } = saveData;
    setLoading(true);
    try {
      // If selecting existing outfit
      if (outfitId && !itemIds) {
        await plannerEventsApi.updateItinerary(outfitEditingData.plannerEventId, outfitEditingData.itineraryId, {
          userId,
          outfitId,
          date: outfitEditingData.date,
          moment: outfitEditingData.moment
        });
      } 
      // If editing items
      else if (itemIds && itemIds.length > 0) {
        // Create a custom outfit with the selected items
        const customOutfitRes = await outfitsApi.create({
          userId,
          name: `Custom - ${new Date(outfitEditingData.date).toLocaleDateString()}`,
          itemIds,
          isAiGenerated: false,
          isEventExclusive: true
        });
        
        // Update itinerary with new custom outfit
        await plannerEventsApi.updateItinerary(outfitEditingData.plannerEventId, outfitEditingData.itineraryId, {
          userId,
          outfitId: customOutfitRes.data,
          date: outfitEditingData.date,
          moment: outfitEditingData.moment
        });
      }

      setOutfitEditingModal(false);
      
      // Update local state
      if (selectedPlannerEvent && selectedPlannerEvent.id === outfitEditingData.plannerEventId) {
        const res = await plannerEventsApi.getByUser(userId);
        const updatedEvent = res.data?.find(e => e.id === outfitEditingData.plannerEventId);
        if (updatedEvent) {
          setSelectedPlannerEvent(updatedEvent);
          setPlannerEvents(prev => prev.map(ev => ev.id === outfitEditingData.plannerEventId ? updatedEvent : ev));
        }
      }
      
      // Refresh outfits list
      fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Save outfit failed');
    } finally {
      setLoading(false);
    }
  };

const onRegenerateItinerary = async (plannerEventId, itineraryId) => {
    setLoading(true);
    try {
      await plannerEventsApi.regenerateItinerary(plannerEventId, itineraryId, { userId });
      // Fetch and update local state
      const res = await plannerEventsApi.getByUser(userId);
      const updatedEvents = Array.isArray(res.data) ? res.data : [];
      setPlannerEvents(updatedEvents);
      
      // Update selected event if needed
      if (selectedPlannerEvent && selectedPlannerEvent.id === plannerEventId) {
        const updatedEvent = updatedEvents.find(e => e.id === plannerEventId);
        if (updatedEvent) setSelectedPlannerEvent(updatedEvent);
      }
    } catch (err) {
      handleApiAlert(err, 'Regenerate itinerary failed');
    } finally {
      setLoading(false);
    }
  };

const openEditItineraryModal = (plannerEventId, itinerary) => {
    setEditItineraryData({
      plannerEventId,
      itineraryId: itinerary.id,
      outfitId: itinerary.outfitId,
      date: itinerary.date ? new Date(itinerary.date).toISOString().split('T')[0] : '',
      moment: itinerary.moment || ''
    });
    setEditItineraryModal(true);
  };

const onUpdateItinerary = async () => {
    if (!editItineraryData.outfitId || !editItineraryData.date || !editItineraryData.moment) {
      alert('Please fill all fields.');
      return;
    }

    setLoading(true);
    try {
      await plannerEventsApi.updateItinerary(editItineraryData.plannerEventId, editItineraryData.itineraryId, {
        userId,
        outfitId: editItineraryData.outfitId,
        date: new Date(editItineraryData.date).toISOString(),
        moment: editItineraryData.moment
      });

      setEditItineraryModal(false);
      setEditItineraryData({ plannerEventId: '', itineraryId: '', outfitId: '', date: '', moment: '' });
      
      // Update local state immediately
      if (selectedPlannerEvent && selectedPlannerEvent.id === editItineraryData.plannerEventId) {
        const updatedEvent = {
          ...selectedPlannerEvent,
          itineraries: selectedPlannerEvent.itineraries?.map(it => 
            it.id === editItineraryData.itineraryId 
              ? { ...it, outfitId: editItineraryData.outfitId, moment: editItineraryData.moment, date: editItineraryData.date }
              : it
          ) || []
        };
        setSelectedPlannerEvent(updatedEvent);
        setPlannerEvents(prev => prev.map(ev => 
          ev.id === editItineraryData.plannerEventId ? updatedEvent : ev
        ));
      }
    } catch (err) {
      handleApiAlert(err, 'Update itinerary failed');
    } finally {
      setLoading(false);
    }
  };

  const onGenerateEventOutfits = async (plannerEventId) => {
    if (!confirm('This will generate AI outfits for each day of the event. Continue?')) {
      return;
    }
    setGeneratingModal(true);
    setGeneratingProgress({ status: 'Generating...', current: 0, total: 0 });
    try {
      const res = await plannerEventsApi.generateOutfits(plannerEventId, { userId });
      setGeneratingProgress({ 
        status: 'Done!', 
        current: res.data.outfitsCreated, 
        total: res.data.daysProcessed 
      });
      
      // Update local state immediately
      if (selectedPlannerEvent && selectedPlannerEvent.id === plannerEventId) {
        const res2 = await plannerEventsApi.getByUser(userId);
        const updatedEvent = res2.data?.find(e => e.id === plannerEventId);
        if (updatedEvent) {
          setSelectedPlannerEvent(updatedEvent);
          setPlannerEvents(prev => prev.map(ev => ev.id === plannerEventId ? updatedEvent : ev));
        }
      }
      
      setTimeout(() => {
        setGeneratingModal(false);
        setGeneratingProgress(null);
      }, 1500);
    } catch (err) {
      handleApiAlert(err, 'Generate outfits failed');
      setGeneratingModal(false);
      setGeneratingProgress(null);
    }
  };

  return (
    <div className="desktop-wrapper">
      <aside className="side-nav">
        <div className="side-nav-top">
          <div className="brand-wrap">
            <div className="brand">W.</div>
            <span className="brand-label">WardrobeManager</span>
          </div>

          <div className="sidebar-user-chip">
            <div className="sidebar-avatar">{userInitials}</div>
            <div className="sidebar-user-meta">
              <span>{userDisplayName}</span>
              <small>{userEmail}</small>
            </div>
          </div>

          <div className="nav-links">
            <button className={`nav-btn ${view === 'dashboard' ? 'active' : ''}`} onClick={() => setView('dashboard')}>
              <span>dashboard</span>
            </button>
            <button className={`nav-btn ${view === 'clothes' ? 'active' : ''}`} onClick={() => setView('clothes')}>
              <span>clothes</span>
              <small>{clothes.length}</small>
            </button>
            <button className={`nav-btn ${view === 'outfits' ? 'active' : ''}`} onClick={() => setView('outfits')}>
              <span>outfits</span>
              <small>{outfits.length}</small>
            </button>
            <button className={`nav-btn ${view === 'planner' ? 'active' : ''}`} onClick={() => setView('planner')}>
              <span>planner</span>
              <small>{plannerEvents.length}</small>
            </button>
            <button className={`nav-btn ${view === 'stats' ? 'active' : ''}`} onClick={() => setView('stats')}>
              <span>stats</span>
              <small>{Math.round(usageRate)}%</small>
            </button>
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <button className="theme-toggle-btn" onClick={toggleTheme}>
            {isDarkMode ? 'light mode' : 'dark mode'}
          </button>
          <button className="logout-btn" onClick={onLogout}>logout</button>
        </div>
      </aside>

      <main className="stage">
        <div className="centered-content">
          <h2 className="soft-title">
            {view === 'dashboard' ? 'dashboard' : view === 'clothes' ? 'your wardrobe' : view === 'outfits' ? 'generated outfits' : view === 'planner' ? 'outfit planner' : 'wardrobe insights'}
          </h2>
          
          {view === 'dashboard' && (
            <WeatherBar
              city={city}
              weatherInfo={weatherInfo}
              onOpenCityModal={() => {
                setSearchTerm('');
                setCityModal(true);
              }}
              onGenerate={() => onGenerate()}
              disabled={loading || clothes.length === 0}
            />
          )}

          {view === 'dashboard' ? (
            <div className="dashboard-layout" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              {/* Today's Events Section */}
              <div className="dashboard-section">
                <h3 className="section-title" style={{ fontSize: '1rem', marginBottom: '15px', color: 'var(--fg)' }}>Today's Events</h3>
                {todaysEvents.length === 0 ? (
                  <div className="empty-state-card" style={{ padding: '20px', textAlign: 'center', background: 'var(--bg-subtle)', borderRadius: '12px', border: '1px dashed var(--border-subtle)' }}>
                    <p style={{ color: 'var(--fg-muted)', fontSize: '0.8rem', margin: 0 }}>No events planned for today.</p>
                    <button className="gen-btn" style={{ marginTop: '10px', padding: '8px 16px', fontSize: '0.7rem' }} onClick={() => setView('planner')}>Go to Planner</button>
                  </div>
                ) : (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '15px' }}>
                    {todaysEvents.map(event => (
                      <div key={event.id} style={{ background: 'var(--card-bg)', borderRadius: '15px', padding: '15px', border: '1px solid var(--border-subtle)', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <div>
                            <h4 style={{ margin: 0, fontSize: '0.9rem', color: 'var(--fg)' }}>{event.name}</h4>
                            <span style={{ fontSize: '0.7rem', color: 'var(--fg-muted)' }}>{event.type} • {event.location}</span>
                          </div>
                          {event.todayItinerary && (
                            <span style={{ fontSize: '0.6rem', background: 'var(--bg-raised)', padding: '4px 8px', borderRadius: '10px', color: 'var(--fg-muted)' }}>{event.todayItinerary.moment}</span>
                          )}
                        </div>
                        
                        {event.todayItinerary?.outfit ? (
                          <div style={{ marginTop: '10px' }}>
                            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>PLANNED OUTFIT</div>
                            <div style={{ display: 'flex', gap: '5px', overflowX: 'auto', paddingBottom: '5px' }}>
                              {event.todayItinerary.outfit.items?.map(item => (
                                <div key={item.id} style={{ width: '40px', height: '40px', borderRadius: '8px', overflow: 'hidden', border: '1px solid var(--border-subtle)', flexShrink: 0 }}>
                                  <img src={item.processedImageUrl} alt={item.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                                </div>
                              ))}
                            </div>
                            <button 
                              onClick={() => onWearOutfit(event.todayItinerary.outfit.id)} 
                              style={{ width: '100%', marginTop: '10px', background: 'var(--accent-bg)', color: 'var(--accent-fg)', border: '1px solid var(--accent)', padding: '8px', borderRadius: '8px', fontSize: '0.7rem', fontWeight: 'bold', cursor: 'pointer' }}
                            >
                              WEAR THIS TODAY
                            </button>
                          </div>
                        ) : (
                          <div style={{ marginTop: '10px', padding: '15px', background: 'var(--bg-subtle)', borderRadius: '10px', textAlign: 'center' }}>
                            <span style={{ fontSize: '0.7rem', color: 'var(--fg-muted)', display: 'block', marginBottom: '10px' }}>No outfit planned for today</span>
                            <button 
                              onClick={() => {
                                setSelectedPlannerEvent(event);
                                const today = new Date();
                                today.setHours(0, 0, 0, 0);
                                const start = new Date(event.startDate);
                                start.setHours(0, 0, 0, 0);
                                const diffTime = Math.abs(today - start);
                                const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
                                setSelectedDayIndex(diffDays);
                                setView('planner');
                              }} 
                              style={{ background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border-subtle)', padding: '6px 12px', borderRadius: '8px', fontSize: '0.6rem', fontWeight: 'bold', cursor: 'pointer' }}
                            >
                              PLAN OUTFIT
                            </button>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Quick Stats & Recent Items */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
                <div className="dashboard-section">
                  <h3 className="section-title" style={{ fontSize: '1rem', marginBottom: '15px', color: 'var(--fg)' }}>Quick Stats</h3>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
                    <div style={{ background: 'var(--card-bg)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)', textAlign: 'center' }}>
                      <div style={{ fontSize: '1.5rem', fontWeight: 'bold', color: 'var(--accent)' }}>{clothes.length}</div>
                      <div style={{ fontSize: '0.7rem', color: 'var(--fg-muted)', textTransform: 'uppercase', letterSpacing: '1px' }}>Items</div>
                    </div>
                    <div style={{ background: 'var(--card-bg)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)', textAlign: 'center' }}>
                      <div style={{ fontSize: '1.5rem', fontWeight: 'bold', color: 'var(--accent)' }}>{outfits.length}</div>
                      <div style={{ fontSize: '0.7rem', color: 'var(--fg-muted)', textTransform: 'uppercase', letterSpacing: '1px' }}>Outfits</div>
                    </div>
                    <div style={{ background: 'var(--card-bg)', padding: '15px', borderRadius: '12px', border: '1px solid var(--border-subtle)', textAlign: 'center', gridColumn: 'span 2' }}>
                      <div style={{ fontSize: '1.5rem', fontWeight: 'bold', color: 'var(--accent)' }}>{Math.round(usageRate)}%</div>
                      <div style={{ fontSize: '0.7rem', color: 'var(--fg-muted)', textTransform: 'uppercase', letterSpacing: '1px' }}>Wardrobe Usage (7d)</div>
                    </div>
                  </div>
                </div>

                <div className="dashboard-section">
                  <h3 className="section-title" style={{ fontSize: '1rem', marginBottom: '15px', color: 'var(--fg)' }}>Recently Added</h3>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '10px' }}>
                    {clothes.slice(0, 6).map(item => (
                      <div key={item.id} style={{ aspectRatio: '1', borderRadius: '10px', overflow: 'hidden', border: '1px solid var(--border-subtle)', cursor: 'pointer' }} onClick={() => { setSelectedItem(item); setView('clothes'); }}>
                        <img src={item.processedImageUrl} alt={item.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                      </div>
                    ))}
                    {clothes.length === 0 && (
                      <div style={{ gridColumn: 'span 3', textAlign: 'center', padding: '20px', color: 'var(--fg-muted)', fontSize: '0.8rem', background: 'var(--bg-subtle)', borderRadius: '10px' }}>
                        No items yet.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ) : view === 'clothes' ? (
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
                  style={{ padding: '12px 30px', fontSize: '0.8rem', background: 'var(--card-bg)', color: 'var(--fg)', border: '2px solid var(--accent)', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}
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
                      <button onClick={() => { setPlanData({ outfitId: o.id, plannerEventId: '', selectedDayIndex: null, moment: '' }); setPlanModal(true); }} style={{ background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border-subtle)', padding: '6px 12px', borderRadius: '8px', fontSize: '0.6rem', fontWeight: 'bold', cursor: 'pointer' }}>PLAN</button>
                      <button onClick={() => onWearOutfit(o.id)} style={{ background: 'var(--accent-bg)', color: 'var(--accent-fg)', border: 'none', padding: '6px 12px', borderRadius: '8px', fontSize: '0.6rem', fontWeight: 'bold', cursor: 'pointer' }}>WEAR TODAY</button>
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
) : view === 'planner' ? (
            <div className="planner-layout">
              {/* Event Rail */}
              <div className="planner-event-rail">
                <div className="planner-rail-header">
                  <span className="rail-title">EVENTS</span>
                  <button 
                    className="rail-add-btn"
                    onClick={() => setCreateEventModal(true)}
                  >
                    +
                  </button>
                </div>
                
                {/* Event Tabs */}
                <div className="event-tabs">
                  <button 
                    className={`event-tab ${plannerEventTab === 'active' ? 'active' : ''}`} 
                    onClick={() => setPlannerEventTab('active')}
                  >
                    ACTIVE ({plannerEvents.length})
                  </button>
                  <button 
                    className={`event-tab ${plannerEventTab === 'archived' ? 'active' : ''}`} 
                    onClick={() => setPlannerEventTab('archived')}
                  >
                    ARCHIVED ({archivedPlannerEvents.length})
                  </button>
                </div>

                <div className="planner-event-list">
                  {plannerEventTab === 'active' ? (
                    plannerEvents.length === 0 ? (
                      <div className="planner-empty-hint">No events yet</div>
                    ) : (
                      plannerEvents.map(event => (
                        <div 
                          key={event.id}
                          className={`planner-event-card ${selectedPlannerEvent?.id === event.id ? 'active' : ''}`}
                          onClick={() => { setSelectedPlannerEvent(event); setSelectedDayIndex(null); fetchEventForecast(event); setPlannerEventTab('active'); }}
                        >
                          <div className="event-card-name">{event.name}</div>
                          <div className="event-card-meta">
                            {new Date(event.startDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                            {event.type && <span className="event-type-badge">{event.type}</span>}
                          </div>
                        </div>
                      ))
                    )
                  ) : (
                    archivedPlannerEvents.length === 0 ? (
                      <div className="planner-empty-hint">No archived events</div>
                    ) : (
                      archivedPlannerEvents.map(event => (
                        <div 
                          key={event.id}
                          className={`planner-event-card ${selectedPlannerEvent?.id === event.id ? 'active' : ''}`}
                          onClick={() => { setSelectedPlannerEvent(event); setSelectedDayIndex(null); fetchEventForecast(event); setPlannerEventTab('archived'); }}
                        >
                          <div className="event-card-name">{event.name}</div>
                          <div className="event-card-meta">
                            {new Date(event.startDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                            {event.type && <span className="event-type-badge archived">{event.type}</span>}
                          </div>
                        </div>
                      ))
                    )
                  )}
                </div>
              </div>

              {/* Day Timeline */}
              <div className="planner-timeline">
                {!selectedPlannerEvent ? (
                  <div className="planner-empty-state">
                    <div className="empty-icon"></div>
                    <div className="empty-text">Select an event to view timeline</div>
                  </div>
                ) : (
                  <>
                    <div className="timeline-header">
                      <h2 className="timeline-title">{selectedPlannerEvent.name}</h2>
                      <div className="timeline-meta">
                        {selectedPlannerEvent.location} • {new Date(selectedPlannerEvent.startDate).toLocaleDateString()} - {new Date(selectedPlannerEvent.endDate).toLocaleDateString()}
                      </div>
                      <div className="timeline-actions">
                        <button 
                          className="gen-btn"
                          onClick={() => onGenerateEventOutfits(selectedPlannerEvent.id)}
                        >
                          GENERATE OUTFITS
                        </button>
                        <button 
                          className="delete-outfit-btn"
                          onClick={() => { if(confirm('Archive this event?')) onArchiveEvent(selectedPlannerEvent.id); }}
                          disabled={loading}
                        >
                          ARCHIVE
                        </button>
                        <button 
                          className="delete-outfit-btn"
                          onClick={() => { if(confirm('Delete this event?')) onDeletePlannerEvent(selectedPlannerEvent.id); setSelectedPlannerEvent(null); }}
                        >
                          DELETE
                        </button>
                      </div>
                    </div>
                    <div className="day-cards-grid">
                      {plannerDays.map((day, idx) => (
                        <div 
                          key={idx}
                          className={`day-card ${selectedDayIndex === idx ? 'selected' : ''} ${day.itinerary ? 'has-outfit' : 'empty'}`}
                          onClick={() => setSelectedDayIndex(idx)}
                        >
                          <div className="day-card-header">
                            <span className="day-number">Day {day.dayNumber}</span>
                            <span className="day-date">{day.date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}</span>
                          </div>
                          {day.weather && (
                            <div className="day-weather-chip">
                              <span className="weather-temp">{Math.round(day.weather.temperature)}°C</span>
                              <span className="weather-condition">{day.weather.condition}</span>
                            </div>
                          )}
                          {day.itinerary ? (
                            <div className="day-outfit-preview">
                              <div className="outfit-mini-grid">
                                {day.itinerary.outfit?.items?.slice(0, 3).map(item => (
                                  <div key={item.id} className="mini-item">
                                    <img src={item.processedImageUrl} alt="" />
                                  </div>
                                ))}
                              </div>
                              <div className="day-moment">{day.itinerary.moment}</div>
                            </div>
                          ) : (
                            <div className="day-empty-hint">No outfit planned</div>
                          )}
                        </div>
                      ))}
                    </div>
                  </>
                )}
              </div>

              {/* Detail Panel */}
              <div className="planner-detail-panel">
                {!selectedPlannerEvent || selectedDayIndex === null ? (
                  <div className="planner-empty-state">
                    <div className="empty-icon"></div>
                    <div className="empty-text">Select a day to view details</div>
                  </div>
                ) : (
                  <div className="day-detail-content">
                    <div className="detail-header">
                      <div>
                        <h3 className="detail-day-title">Day {selectedDayIndex + 1}</h3>
                        <p className="detail-date">{plannerDays[selectedDayIndex]?.date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}</p>
                      </div>
                    </div>

                    {selectedDayItinerary ? (
                      <>
                        <div className="detail-outfit">
                          <h4 className="detail-section-title">{selectedDayItinerary.outfit?.name || 'Outfit'}</h4>
                          <div className="outfit-items-grid">
                            {selectedDayItinerary.outfit?.items?.map(item => (
                              <div key={item.id} className="detail-item-card" onClick={() => setSelectedItem(item)}>
                                <img src={item.processedImageUrl} alt="" />
                                <span className="item-name">{item.name}</span>
                              </div>
                            ))}
                          </div>
                          <div className="detail-moment">
                            <span className="moment-label">Moment:</span>
                            <span className="moment-value">{selectedDayItinerary.moment}</span>
                          </div>
                          <div className="detail-actions">
                            <button
                              className="action-btn"
                              onClick={() => onRegenerateItinerary(selectedPlannerEvent.id, selectedDayItinerary.id)}
                            >
                              Regenerate
                            </button>
                            <button
                              className="action-btn secondary"
                              onClick={() => openOutfitEditingModal(selectedPlannerEvent.id, selectedDayItinerary, plannerDays[selectedDayIndex], selectedDayIndex)}
                            >
                              Edit Outfit
                            </button>
                            <button
                              className="action-btn secondary"
                              onClick={() => openEditItineraryModal(selectedPlannerEvent.id, selectedDayItinerary)}
                            >
                              Edit Details
                            </button>
                            <button
                              className="action-btn danger"
                              onClick={() => { onDeleteItinerary(selectedPlannerEvent.id, selectedDayItinerary.id); setSelectedDayIndex(null); }}
                            >
                              Remove
                            </button>
                          </div>
                        </div>
                      </>
                    ) : (
                      <div className="detail-empty">
                        <p>No outfit planned for this day</p>
                        <button 
                          className="gen-btn"
                          onClick={() => { setPlanData({ outfitId: null, plannerEventId: selectedPlannerEvent.id, selectedDayIndex: selectedDayIndex, moment: '' }); setPlanModal(true); }}
                        >
                          PLAN OUTFIT
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="stats-layout">
              <div className="profile-stats-header">
                <div className="profile-card">
                  <div className="profile-main">
                    <div className="profile-avatar-large">{userInitials}</div>
                    <div>
                      <h3 className="profile-name">{userDisplayName}</h3>
                      <p className="profile-email">{userEmail}</p>
                    </div>
                  </div>

                  <div className="profile-meta-grid">
                    <div className="profile-meta-item">
                      <span>city</span>
                      <strong>{city}</strong>
                    </div>
                    <div className="profile-meta-item">
                      <span>weather</span>
                      <strong>{weatherSummary}</strong>
                    </div>
                    <div className="profile-meta-item">
                      <span>member since</span>
                      <strong>{memberSince}</strong>
                    </div>
                  </div>
                </div>

                <div className="profile-kpi-grid">
                  <div className="profile-kpi-card">
                    <span>wardrobe items</span>
                    <strong>{clothes.length}</strong>
                  </div>
                  <div className="profile-kpi-card">
                    <span>saved outfits</span>
                    <strong>{outfits.length}</strong>
                  </div>
                  <div className="profile-kpi-card">
                    <span>ai outfits</span>
                    <strong>{aiOutfitCount}</strong>
                  </div>
                  <div className="profile-kpi-card">
                    <span>custom outfits</span>
                    <strong>{customOutfitCount}</strong>
                  </div>
                  <div className="profile-kpi-card full">
                    <span>tracked styles</span>
                    <strong>{trackedStyles}</strong>
                  </div>
                </div>
              </div>

              <StatsSection userId={userId} />
            </div>
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
            <div style={{ textAlign: 'center', background: 'var(--bg-subtle)', borderRadius: '20px', padding: '15px', border: '1px solid var(--border-subtle)' }}>
              <img src={selectedItem.processedImageUrl} alt="" style={{ maxWidth: '100%', maxHeight: '350px', borderRadius: '15px', objectFit: 'contain' }} />
            </div>

            {editItemMode ? (
              <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>NAME</span>
                  <input className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.name} onChange={e => setEditItemData({...editItemData, name: e.target.value})} />
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>TYPE</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={typeof editItemData.type === 'number' ? CLOTHING_TYPES[editItemData.type] : editItemData.type} onChange={e => setEditItemData({...editItemData, type: CLOTHING_TYPES.indexOf(e.target.value)})}>
                    {CLOTHING_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>COLOR</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.color} onChange={e => setEditItemData({...editItemData, color: e.target.value})}>
                    {COLORS.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>
                <div>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>GENDER</span>
                  <select className="name-input" style={{ fontSize: '0.8rem', padding: '8px' }} value={editItemData.gender} onChange={e => setEditItemData({...editItemData, gender: e.target.value})}>
                    {GENDERS.map(g => <option key={g} value={g}>{g}</option>)}
                  </select>
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '10px' }}>SEASON (MULTI-SELECT)</span>
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
                            padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid var(--accent)' : '1px solid var(--border-muted)',
                            background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', color: isSelected ? 'var(--accent-fg)' : 'var(--fg-muted)', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                          }}
                        >{s.toUpperCase()}</button>
                      );
                    })}
                  </div>
                </div>
                <div style={{ gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '10px' }}>USAGE / STYLE (MULTI-SELECT)</span>
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
                            padding: '6px 12px', fontSize: '0.6rem', borderRadius: '8px', border: isSelected ? '1px solid var(--accent)' : '1px solid var(--border-muted)',
                            background: isSelected ? 'var(--accent-bg)' : 'var(--card-bg)', color: isSelected ? 'var(--accent-fg)' : 'var(--fg-muted)', cursor: 'pointer', fontFamily: 'JetBrains Mono'
                          }}
                        >{u.toUpperCase()}</button>
                      );
                    })}
                  </div>
                </div>
              </div>
            ) : (
              <div className="inspect-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
                <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>TYPE</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{CLOTHING_TYPES[selectedItem.type] || selectedItem.type}</span>
                </div>
                <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>COLOR</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.color?.toUpperCase()}</span>
                </div>
                <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>GENDER</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.gender?.toUpperCase() || 'UNISEX'}</span>
                </div>
                <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>SEASON</span>
                  <span className="robotic-text" style={{ fontSize: '0.75rem' }}>{selectedItem.season?.toUpperCase() || 'ANY'}</span>
                </div>
                <div style={{ background: 'var(--bg-subtle)', padding: '12px', borderRadius: '12px', border: '1px solid var(--border-subtle)', gridColumn: 'span 2' }}>
                  <span style={{ fontSize: '0.55rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '4px' }}>USAGE</span>
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
                      season: toStringArray(selectedItem.season),
                      usage: toStringArray(selectedItem.usage)
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
              <button key={style} onClick={() => executeGeneration(style)} style={{ padding: '20px', background: 'var(--card-bg)', color: 'var(--fg)', border: '1px solid var(--border-subtle)', borderRadius: '15px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
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
              <button key={idx} onClick={() => { handleCityChange(c.name); setCityModal(false); }} style={{ width: '100%', padding: '10px', textAlign: 'left', background: 'none', border: '1px solid var(--border-subtle)', marginBottom: '5px', color: 'var(--fg)' }}>
                {c.name}, {c.country}
              </button>
            ))}
          </div>
        </div>
      </Modal>

      <Modal isOpen={planModal} onClose={() => setPlanModal(false)} title="PLAN OUTFIT TO EVENT" size="medium">
        <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>SELECT EVENT</span>
            <select 
              className="name-input" 
              value={planData.plannerEventId} 
              onChange={e => setPlanData({...planData, plannerEventId: e.target.value, selectedDayIndex: null})}
              style={{ width: '100%' }}
            >
              <option value="">-- Select Event --</option>
              {plannerEvents.map(event => (
                <option key={event.id} value={event.id}>
                  {event.name} ({new Date(event.startDate).toLocaleDateString()} - {new Date(event.endDate).toLocaleDateString()})
                </option>
              ))}
            </select>
          </div>
          
          {planData.plannerEventId && (
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>SELECT DAY</span>
              <select 
                className="name-input" 
                value={planData.selectedDayIndex !== null ? planData.selectedDayIndex : ''}
                onChange={e => setPlanData({...planData, selectedDayIndex: parseInt(e.target.value)})}
                style={{ width: '100%' }}
              >
                <option value="">-- Select Day --</option>
                {currentEventDays.map(day => (
                  <option key={day.index} value={day.index}>
                    {day.label}
                  </option>
                ))}
              </select>
            </div>
          )}
          
          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>MOMENT (e.g. Morning, Dinner, Flight)</span>
            <input 
              type="text" 
              className="name-input" 
              value={planData.moment} 
              onChange={e => setPlanData({...planData, moment: e.target.value})}
              placeholder="Enter moment..."
            />
          </div>
          
          <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
            <Button label="PLAN" onClick={onPlanOutfit} loading={loading} disabled={!planData.plannerEventId || planData.selectedDayIndex === null || !planData.moment} />
            <Button label="CANCEL" variant="secondary" onClick={() => setPlanModal(false)} />
          </div>
        </div>
      </Modal>

      <Modal isOpen={editItineraryModal} onClose={() => { setEditItineraryModal(false); setEditItineraryData({ plannerEventId: '', itineraryId: '', outfitId: '', date: '', moment: '' }); }} title="EDIT ITINERARY" size="medium">
        <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
          {/* Current Outfit Display (read-only) */}
          {editItineraryData.outfitId && (
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '8px' }}>CURRENT OUTFIT</span>
              <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', padding: '10px', background: 'var(--bg-subtle)', borderRadius: '12px' }}>
                {(() => {
                  const outfit = outfits.find(o => o.id === editItineraryData.outfitId);
                  return outfit ? (
                    <>
                      <div style={{ fontSize: '0.75rem', fontWeight: 'bold', width: '100%', marginBottom: '4px' }}>{outfit.name}</div>
                      {outfit.items?.map(item => (
                        <div key={item.id} style={{ width: '50px', height: '50px', borderRadius: '8px', overflow: 'hidden', border: '1px solid var(--border-subtle)' }}>
                          <img src={item.processedImageUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        </div>
                      ))}
                    </>
                  ) : <span style={{ fontSize: '0.7rem', color: 'var(--fg-muted)' }}>No items</span>;
                })()}
              </div>
              <div style={{ fontSize: '0.6rem', color: 'var(--fg-faint)', marginTop: '8px' }}>To change outfit, please remove this one and plan a new outfit</div>
            </div>
          )}

          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>DATE</span>
            <input
              type="date"
              className="name-input"
              value={editItineraryData.date}
              onChange={(e) => setEditItineraryData({ ...editItineraryData, date: e.target.value })}
            />
          </div>

          <div>
            <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>MOMENT</span>
            <input
              type="text"
              className="name-input"
              value={editItineraryData.moment}
              onChange={(e) => setEditItineraryData({ ...editItineraryData, moment: e.target.value })}
              placeholder="Morning, Dinner, Travel..."
            />
          </div>

          <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
            <Button label="SAVE" onClick={onUpdateItinerary} loading={loading} disabled={!editItineraryData.date || !editItineraryData.moment} />
            <Button label="CANCEL" variant="secondary" onClick={() => setEditItineraryModal(false)} />
          </div>
        </div>
      </Modal>

{/* CREATE EVENT MODAL - 3-Step Wizard */}
      <Modal 
        isOpen={createEventModal} 
        onClose={() => { 
          setCreateEventModal(false); 
          setWizardStep(0); 
          setWizardPreview(null); 
          setCreateEventData({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '' }); 
        }} 
        title={wizardStep === 0 ? "CREATE NEW EVENT" : "EVENT PREVIEW"} 
        size="large"
      >
        {/* Step Indicator */}
        <div style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '20px', padding: '0 10px' }}>
          {[0, 1].map(step => (
            <div key={step} style={{ 
              width: '80px', 
              height: '4px', 
              borderRadius: '2px', 
              background: step <= wizardStep ? 'var(--accent)' : 'var(--border-subtle)',
              transition: 'all 0.3s'
            }} />
          ))}
        </div>

        {/* Step 0: Event Details */}
        {wizardStep === 0 && (
          <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT NAME</span>
              <input 
                type="text" 
                className="name-input" 
                value={createEventData.name} 
                onChange={e => setCreateEventData({...createEventData, name: e.target.value})}
                placeholder="e.g. Summer Vacation 2026"
                autoFocus
              />
            </div>
            
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>EVENT TYPE</span>
              <select 
                className="name-input" 
                value={createEventData.type} 
                onChange={e => setCreateEventData({...createEventData, type: e.target.value})}
                style={{ width: '100%' }}
              >
                <option value="Vacation">Vacation</option>
                <option value="Business Trip">Business Trip</option>
                <option value="Wedding">Wedding</option>
                <option value="Party">Party</option>
                <option value="Meeting">Meeting</option>
                <option value="Date">Date</option>
                <option value="Weekend">Weekend</option>
                <option value="Other">Other</option>
              </select>
            </div>
            
            <div>
              <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>LOCATION</span>
              <div style={{ position: 'relative' }}>
                <input 
                  type="text" 
                  className="name-input" 
                  value={createEventData.location} 
                  onChange={e => {
                    setCreateEventData({...createEventData, location: e.target.value});
                    setEventLocationSearch(e.target.value);
                  }}
                  placeholder="e.g. Paris, France"
                />
                {eventLocationSuggestions.length > 0 && (
                  <div style={{ 
                    position: 'absolute', 
                    top: '100%', 
                    left: 0, 
                    right: 0, 
                    background: 'var(--card-bg)', 
                    border: '1px solid var(--border-subtle)', 
                    borderRadius: '8px',
                    maxHeight: '150px',
                    overflowY: 'auto',
                    zIndex: 1000,
                    marginTop: '4px'
                  }}>
                    {eventLocationSuggestions.map((city, idx) => (
                      <button 
                        key={idx}
                        onClick={() => {
                          setCreateEventData({...createEventData, location: `${city.name}, ${city.country}`});
                          setEventLocationSearch('');
                          setEventLocationSuggestions([]);
                        }}
                        style={{ 
                          width: '100%', 
                          padding: '8px 12px', 
                          textAlign: 'left', 
                          background: 'none', 
                          border: 'none',
                          borderBottom: '1px solid var(--border-subtle)',
                          color: 'var(--fg)',
                          cursor: 'pointer',
                          fontSize: '0.8rem'
                        }}
                      >
                        {city.name}, {city.country}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>
            
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
              <div>
                <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>START DATE</span>
                <input 
                  type="date" 
                  className="name-input" 
                  value={createEventData.startDate} 
                  onChange={e => {
                    const newStartDate = e.target.value;
                    setCreateEventData({
                      ...createEventData, 
                      startDate: newStartDate,
                      endDate: createEventData.endDate || newStartDate
                    });
                  }}
                />
              </div>
              <div>
                <span style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', display: 'block', marginBottom: '5px' }}>END DATE</span>
                <input 
                  type="date" 
                  className="name-input" 
                  value={createEventData.endDate} 
                  onChange={e => setCreateEventData({...createEventData, endDate: e.target.value})}
                />
              </div>
            </div>
            
            <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
              <Button 
                label="NEXT PREVIEW" 
                onClick={onPreviewEvent} 
                loading={wizardLoading} 
                disabled={!createEventData.name || !createEventData.location || !createEventData.startDate || !createEventData.endDate} 
              />
              <Button 
                label="CANCEL" 
                variant="secondary" 
                onClick={() => { 
                  setCreateEventModal(false); 
                  setWizardStep(0); 
                  setWizardPreview(null); 
                  setCreateEventData({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '' }); 
                }} 
              />
            </div>
          </div>
        )}

        {/* Step 1: Preview with Weather */}
        {wizardStep === 1 && wizardPreview && (
          <div style={{ padding: '10px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
            <div style={{ 
              background: 'var(--bg-subtle)', 
              padding: '15px', 
              borderRadius: '12px', 
              border: '1px solid var(--border-subtle)',
              marginBottom: '10px'
            }}>
              <div style={{ fontWeight: 'bold', fontSize: '0.9rem', marginBottom: '5px' }}>{createEventData.name}</div>
              <div style={{ fontSize: '0.75rem', color: 'var(--fg-muted)' }}>
                {createEventData.type} • {createEventData.location}
              </div>
              <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginTop: '5px' }}>
                {new Date(createEventData.startDate).toLocaleDateString()} - {new Date(createEventData.endDate).toLocaleDateString()}
              </div>
            </div>

            <div style={{ fontSize: '0.7rem', color: 'var(--fg-faint)', marginBottom: '5px' }}>
              WEATHER FORECAST ({wizardPreview.location})
            </div>
            
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))', 
              gap: '10px',
              maxHeight: '250px',
              overflowY: 'auto'
            }}>
              {wizardPreview.days.map((day, idx) => (
                <div key={idx} style={{ 
                  background: 'var(--card-bg)', 
                  padding: '10px', 
                  borderRadius: '10px', 
                  border: '1px solid var(--border-subtle)',
                  textAlign: 'center'
                }}>
                  <div style={{ fontSize: '0.6rem', color: 'var(--fg-muted)', marginBottom: '4px' }}>
                    {day.date.toLocaleDateString(undefined, { weekday: 'short' })}
                  </div>
                  <div style={{ fontSize: '0.75rem', fontWeight: 'bold' }}>
                    Day {day.dayNumber}
                  </div>
                  <div style={{ 
                    background: 'var(--accent-bg)', 
                    padding: '4px 8px', 
                    borderRadius: '8px',
                    marginTop: '6px'
                  }}>
                    <div style={{ fontSize: '0.8rem', fontWeight: 'bold', color: 'var(--accent-fg)' }}>
                      {Math.round(day.weather?.temperature || 20)}°C
                    </div>
                    <div style={{ fontSize: '0.5rem', color: 'var(--accent-fg)', opacity: 0.8 }}>
                      {day.weather?.condition || 'N/A'}
                    </div>
                  </div>
                </div>
              ))}
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
              <Button label="BACK" variant="secondary" onClick={() => setWizardStep(0)} />
              <Button label="CREATE EVENT" onClick={onCreatePlannerEvent} loading={loading} />
            </div>
          </div>
        )}
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

      {/* GENERATING OUTFITS MODAL */}
      <Modal isOpen={generatingModal} onClose={() => setGeneratingModal(false)} title="GENERATING OUTFITS" size="small">
        <div style={{ padding: '30px', textAlign: 'center' }}>
          {generatingProgress && (
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

      {/* OUTFIT EDITING MODAL */}
      <OutfitEditingModal
        isOpen={outfitEditingModal}
        onClose={() => setOutfitEditingModal(false)}
        onSave={onSaveOutfitEdit}
        clothes={clothes}
        outfits={outfits}
        currentOutfit={selectedDayItinerary?.outfit}
        currentItinerary={selectedDayItinerary}
        dayInfo={selectedDayIndex !== null ? plannerDays[selectedDayIndex] : null}
        loading={loading}
      />
    </div>
  );
};

export default DashboardPage;
