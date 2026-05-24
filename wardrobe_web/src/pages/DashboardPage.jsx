import React, { useCallback, useMemo, useState, useEffect, useRef } from 'react';
import Button from '../components/Button';
import Modal from '../components/Modal';
import PackSmartModal from '../components/modals/PackSmartModal';
import CreateEventModal from '../components/modals/CreateEventModal';
import EditEventModal from '../components/modals/EditEventModal';
import CustomOutfitModal from '../components/modals/CustomOutfitModal';
import PlanOutfitModal from '../components/modals/PlanOutfitModal';
import EditItineraryModal from '../components/modals/EditItineraryModal';
import DayPreviewModal from '../components/modals/DayPreviewModal';
import GeneratingModal from '../components/modals/GeneratingModal';
import UploadModal from '../components/modals/UploadModal';
import EditOutfitModal from '../components/modals/EditOutfitModal';
import ItemInspectModal from '../components/modals/ItemInspectModal';
import StyleSelectionModal from '../components/modals/StyleSelectionModal';
import AiSuggestionModal from '../components/modals/AiSuggestionModal';
import ValidationModal from '../components/modals/ValidationModal';
import CitySelectionModal from '../components/modals/CitySelectionModal';
import OutfitEditingModal from '../components/OutfitEditingModal';
import StatsSection from '../components/StatsSection';
import WeatherAlertNotice from '../components/WeatherAlertNotice';
import WeatherBar from '../components/WeatherBar';
import { clothingApi, geoApi, outfitsApi, plannerEventsApi, statsApi } from '../services/wardrobeApi';
import { COLORS, CLOTHING_TYPES, GENDERS, SEASONS, USAGES, EVENT_MOMENTS } from '../constants/wardrobe';
import { getErrorMessage } from '../utils/errors';
import { toCsv, toStringArray, toTypeIndex } from '../utils/wardrobeTransforms';
import { useTheme } from '../contexts/ThemeContext';

const DAY_IN_MS = 24 * 60 * 60 * 1000;

const toDayStart = (value) => {
  const date = new Date(value);
  date.setHours(0, 0, 0, 0);
  return date;
};

const toDayEnd = (value) => {
  const date = new Date(value);
  date.setHours(23, 59, 59, 999);
  return date;
};

const toDayKey = (value) => {
  const date = toDayStart(value);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
};

const findItineraryForDate = (event, targetDate) => {
  const targetKey = toDayKey(targetDate);
  return event?.itineraries?.find((itinerary) => toDayKey(itinerary.date) === targetKey) || null;
};

const getDayOffset = (eventStartDate, targetDate) => {
  const start = toDayStart(eventStartDate).getTime();
  const target = toDayStart(targetDate).getTime();
  return Math.max(0, Math.round((target - start) / DAY_IN_MS));
};

const DashboardPage = ({ user, onLogout }) => {
  const { isDarkMode, toggleTheme } = useTheme();
  const [genericForecast, setGenericForecast] = useState([]);
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [outfitFilter, setOutfitFilter] = useState('all'); // 'all', 'favorites'
  const [plannerEvents, setPlannerEvents] = useState([]);
  const [usageRate, setUsageRate] = useState(0);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('dashboard');
  const [previewDay, setPreviewDay] = useState(null);
  const [selectedItem, setSelectedItem] = useState(null);
  const [editItemMode, setEditItemMode] = useState(false);
  const [editItemData, setEditItemData] = useState(null);
  
  const [uploadModal, setUploadModal] = useState(false);
  const [uploadData, setUploadData] = useState([]);
  
  const [validationModal, setValidationModal] = useState(false);
  const [validationData, setValidationData] = useState(null);
  const [validationQueue, setValidationQueue] = useState([]);
  const [originalPredictions, setOriginalPredictions] = useState(null);
  const [currentStep, setCurrentStep] = useState(0); 
  const [validationSearchTerm, setValidationSearchTerm] = useState('');
  
  const [editModal, setEditModal] = useState(false);
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [] });

  const [planModal, setPlanModal] = useState(false);
  const [planData, setPlanData] = useState({ outfitId: null, plannerEventId: '', selectedDayIndex: null, moment: '' });

  const [city, setCity] = useState(localStorage.getItem('userCity') || 'Detecting...');
  const [cityModal, setCityModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [weatherInfo, setWeatherInfo] = useState(null);
  const [weatherAlert, setWeatherAlert] = useState(null);
  const [citySuggestions, setCitySuggestions] = useState([]);
  const [styleSelectionModal, setStyleSelectionModal] = useState(false);
  const [generationContext, setGenerationContext] = useState(null);

  const [packSmartModal, setPackSmartModal] = useState(false);
  const [packSmartData, setPackSmartData] = useState(null);
  const [packedItems, setPackedItems] = useState([]);

  // Event weather forecasts
  const [eventForecasts, setEventForecasts] = useState({});

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
    const today = toDayStart(new Date());

    return plannerEvents
      .filter((event) => today >= toDayStart(event.startDate) && today <= toDayEnd(event.endDate))
      .map((event) => ({
        ...event,
        todayItinerary: findItineraryForDate(event, today)
      }));
  }, [plannerEvents]);

  const todaysEventSummary = useMemo(() => {
    const plannedCount = todaysEvents.filter((event) => event.todayItinerary?.outfitId || event.todayItinerary?.outfit).length;
    const totalEvents = todaysEvents.length;

    return {
      totalEvents,
      plannedCount,
      missingCount: Math.max(totalEvents - plannedCount, 0)
    };
  }, [todaysEvents]);

  const upcomingWeekDays = useMemo(() => {
    const today = toDayStart(new Date());

    return Array.from({ length: 7 }, (_, index) => {
      const currentDate = new Date(today);
      currentDate.setDate(today.getDate() + index);

      const mappedEvents = plannerEvents
        .filter((event) => currentDate >= toDayStart(event.startDate) && currentDate <= toDayEnd(event.endDate))
        .map((event) => {
          const itinerary = findItineraryForDate(event, currentDate);
          return { event, itinerary };
        });

      const totalEvents = mappedEvents.length;
      const plannedCount = mappedEvents.filter((entry) => entry.itinerary?.outfitId || entry.itinerary?.outfit).length;
      const primaryEntry = mappedEvents.find((entry) => !(entry.itinerary?.outfitId || entry.itinerary?.outfit)) || mappedEvents[0] || null;
      const primaryEvent = primaryEntry?.event || null;
      const primaryItinerary = primaryEntry?.itinerary || null;

      const forecastCandidates = primaryEvent ? eventForecasts[primaryEvent.id] || [] : [];
      let matchedForecast = forecastCandidates.find((forecast) => toDayKey(forecast.date) === toDayKey(currentDate));
      
      // If we don't have an event forecast for this day, try using the generic city forecast (up to 5 days)
      if (!matchedForecast && genericForecast && genericForecast.length > 0) {
        matchedForecast = genericForecast.find((forecast) => toDayKey(forecast.date) === toDayKey(currentDate));
      }

      const fallbackForecast = index === 0 && weatherInfo
        ? { temperature: weatherInfo.temperature, condition: weatherInfo.condition }
        : null;

      const status = totalEvents === 0
        ? 'free'
        : plannedCount === totalEvents
          ? 'planned'
          : 'needs-plan';

      return {
        dayKey: toDayKey(currentDate),
        date: currentDate,
        isToday: index === 0,
        weekdayLabel: currentDate.toLocaleDateString(undefined, { weekday: 'short' }),
        dayLabel: currentDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
        totalEvents,
        plannedCount,
        status,
        primaryEvent,
        primaryItinerary,
        allEvents: mappedEvents,
        weather: matchedForecast || fallbackForecast
      };
    });
  }, [plannerEvents, eventForecasts, weatherInfo, genericForecast]);

  const weekDaysWithEvents = useMemo(
    () => upcomingWeekDays.filter((day) => day.totalEvents > 0),
    [upcomingWeekDays]
  );

  const weekReadyDays = useMemo(
    () => weekDaysWithEvents.filter((day) => day.status === 'planned').length,
    [weekDaysWithEvents]
  );

  const [editItineraryModal, setEditItineraryModal] = useState(false);
const [editItineraryData, setEditItineraryData] = useState({
    plannerEventId: '',
    itineraryId: '',
    outfitId: '',
    date: '',
    moment: ''
  });

  const [createEventModal, setCreateEventModal] = useState(false);
  const [createEventData, setCreateEventData] = useState({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [] });
  const [editEventModal, setEditEventModal] = useState(false);
  const [editEventData, setEditEventData] = useState({ id: '', name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [] });
  const [eventLocationSearch, setEventLocationSearch] = useState('');
  const [eventLocationSuggestions, setEventLocationSuggestions] = useState([]);

  const [generatingModal, setGeneratingModal] = useState(false);
  const [generatingProgress, setGeneratingProgress] = useState(null);

  const [aiModal, setAiModal] = useState(false);
  const [aiData, setAiData] = useState(null);
  
  // Custom Outfit State
  const [customOutfitModal, setCustomOutfitModal] = useState(false);
  const [customOutfitData, setCustomOutfitData] = useState({ name: '', itemIds: [], tags: [] });
  const [customOutfitTab, setCustomOutfitTab] = useState(0); 

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
    mode: 'edit',
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

  const firstName = useMemo(() => userDisplayName.split(/\s+/).filter(Boolean)[0] || 'there', [userDisplayName]);

  const greetingLabel = useMemo(() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'good morning';
    if (hour < 18) return 'good afternoon';
    return 'good evening';
  }, []);

  const todaysReadinessPercent = useMemo(() => {
    if (todaysEventSummary.totalEvents === 0) return 100;
    return Math.round((todaysEventSummary.plannedCount / todaysEventSummary.totalEvents) * 100);
  }, [todaysEventSummary]);

  const quickActions = useMemo(() => ([
    {
      id: 'add-item',
      label: 'Add item',
      hint: 'Upload and classify a new piece',
      disabled: false
    },
    {
      id: 'create-outfit',
      label: 'Create outfit',
      hint: 'Build a custom look manually',
      disabled: clothes.length === 0
    },
    {
      id: 'plan-today',
      label: 'Plan today',
      hint: 'Assign outfits for current events',
      disabled: todaysEventSummary.totalEvents === 0
    },
    {
      id: 'generate-today',
      label: 'Generate AI look',
      hint: 'Auto-generate with weather context',
      disabled: loading || clothes.length === 0
    },
    {
      id: 'open-planner',
      label: 'Open planner',
      hint: 'Manage day-by-day event timelines',
      disabled: false
    }
  ]), [clothes.length, loading, todaysEventSummary.totalEvents]);

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
      
      // Also fetch generic 5-day forecast for the city so we can display it on the upcoming 7 days timeline
      const forecastRes = await outfitsApi.getForecast(city, 5, new Date());
      if (forecastRes.data?.forecasts) {
        setGenericForecast(forecastRes.data.forecasts);
      }
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
       const payload = res.data || {};
       const events = Array.isArray(payload.plannerEvents) ? payload.plannerEvents : payload;
       const nextWeatherAlert = payload.weatherAlert;
       setPlannerEvents(Array.isArray(events) ? events : []);
       setWeatherAlert(nextWeatherAlert ?? null);
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

  const handleTestAlert = async () => {
    if (!userId) return;
    try {
      const res = await plannerEventsApi.getTestAlert(userId);
      if (res.data) {
        setWeatherAlert(res.data);
      } else {
        alert('No active events found to generate a test alert.');
      }
    } catch (err) {
      handleApiAlert(err, 'Failed to fetch test alert.');
    }
  };

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

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      const filesArray = Array.from(e.target.files);
      const initialUploadData = filesArray.map(file => ({
        file,
        name: file.name.split('.')[0]
      }));
      setUploadData(initialUploadData);
      setUploadModal(true);
    }
    e.target.value = null;
  };

  const onUpload = async () => {
    setLoading(true);
    try {
      const results = [];
      for (const item of uploadData) {
        const fd = new FormData();
        fd.append('File', item.file);
        fd.append('UserId', userId);
        fd.append('Name', item.name);
        
        const res = await clothingApi.process(fd);
        results.push(res.data);
      }
      
      if (results.length > 0) {
        setValidationQueue(results);
        
        const firstItem = results[0];
        setOriginalPredictions({
          type: firstItem.type,
          color: firstItem.color,
          gender: firstItem.gender,
          season: firstItem.season,
          usage: firstItem.usage
        });

        setValidationData({
          ...firstItem,
          season: firstItem.season ? [firstItem.season] : [],
          usage: firstItem.usage ? [firstItem.usage] : []
        });

        setCurrentStep(0);
        setUploadModal(false);
        setValidationModal(true);
      }
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
      
      const newQueue = validationQueue.slice(1);
      setValidationQueue(newQueue);
      
      if (newQueue.length > 0) {
        const nextItem = newQueue[0];
        setValidationData({
          ...nextItem,
          season: nextItem.season ? [nextItem.season] : [],
          usage: nextItem.usage ? [nextItem.usage] : []
        });
        setOriginalPredictions({
          type: nextItem.type,
          color: nextItem.color,
          gender: nextItem.gender,
          season: nextItem.season,
          usage: nextItem.usage
        });
        setCurrentStep(0);
      } else {
        setValidationModal(false);
        fetchClothes();
      }
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

  const onToggleFavorite = async (outfit) => {
    try {
      // Optimistic update
      setOutfits(prev => prev.map(o => o.id === outfit.id ? { ...o, isFavorite: !o.isFavorite } : o));
      await outfitsApi.toggleFavorite(outfit.id);
    } catch (err) {
      // Revert on fail
      setOutfits(prev => prev.map(o => o.id === outfit.id ? { ...o, isFavorite: outfit.isFavorite } : o));
      handleApiAlert(err, 'Failed to toggle favorite');
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
        tags: customOutfitData.tags || [],
        isAiGenerated: false
      });
      setCustomOutfitModal(false);
      setCustomOutfitData({ name: '', itemIds: [], tags: [] });
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
              setCreateEventData({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [] });
      fetchPlannerEvents();
    } catch (err) {
      handleApiAlert(err, 'Create event failed');
    }
    finally { setLoading(false); }
  };

  const onUpdatePlannerEvent = async () => {
    if (!editEventData.name || !editEventData.location || !editEventData.startDate || !editEventData.endDate) {
      alert("Please fill all fields.");
      return;
    }
    
    setLoading(true);
    try {
      await plannerEventsApi.update(editEventData.id, {
        userId,
        ...editEventData,
        startDate: new Date(editEventData.startDate).toISOString(),
        endDate: new Date(editEventData.endDate).toISOString()
      });
      
      setEditEventModal(false);
      fetchPlannerEvents();
      
      // Update selected event if it's the one being edited
      if (selectedPlannerEvent && selectedPlannerEvent.id === editEventData.id) {
       const res = await plannerEventsApi.getByUser(userId);
       const payload = res.data || {};
       const events = Array.isArray(payload.plannerEvents) ? payload.plannerEvents : payload;
       const nextWeatherAlert = payload.weatherAlert;
       const updatedEvent = events?.find(e => e.id === editEventData.id);
       if (nextWeatherAlert) {
         setWeatherAlert(nextWeatherAlert);
       }
       if (updatedEvent) {
         setSelectedPlannerEvent(updatedEvent);
       }
      }
    } catch (err) {
      handleApiAlert(err, 'Update event failed');
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

const openOutfitEditingModal = (plannerEventId, itinerary, dayInfo, dayIndex, mode = 'edit') => {
    setOutfitEditingData({
      plannerEventId,
      itineraryId: itinerary?.id || null,
      outfitId: itinerary?.outfitId || null,
      date: itinerary?.date || dayInfo?.date?.toISOString() || null,
      moment: itinerary?.moment || '',
      dayIndex,
      mode,
    });
    setOutfitEditingModal(true);
  };

const onSaveOutfitEdit = async (saveData) => {
    const { outfitId, itemIds, moment } = saveData;
    setLoading(true);
    try {
      let finalOutfitId = outfitId;

      // If editing items, create a custom outfit first
      if (itemIds && itemIds.length > 0) {
        const customOutfitRes = await outfitsApi.create({
          userId,
          name: `Custom - ${new Date(outfitEditingData.date).toLocaleDateString()}`,
          itemIds,
          isAiGenerated: false,
          isEventExclusive: true
        });
        finalOutfitId = customOutfitRes.data;
      }

      if (outfitEditingData.mode === 'plan') {
        // Add new itinerary
        await plannerEventsApi.addItinerary(outfitEditingData.plannerEventId, {
          userId,
          outfitId: finalOutfitId,
          date: outfitEditingData.date,
          moment: moment || outfitEditingData.moment
        });
      } else {
        // Update existing itinerary
        await plannerEventsApi.updateItinerary(outfitEditingData.plannerEventId, outfitEditingData.itineraryId, {
          userId,
          outfitId: finalOutfitId,
          date: outfitEditingData.date,
          moment: moment || outfitEditingData.moment
        });
      }

      setOutfitEditingModal(false);
      
      // Update local state
      if (selectedPlannerEvent && selectedPlannerEvent.id === outfitEditingData.plannerEventId) {
       const res = await plannerEventsApi.getByUser(userId);
       const payload = res.data || {};
       const events = Array.isArray(payload.plannerEvents) ? payload.plannerEvents : payload;
       const nextWeatherAlert = payload.weatherAlert;
       const updatedEvent = events?.find(e => e.id === outfitEditingData.plannerEventId);
       if (nextWeatherAlert) {
         setWeatherAlert(nextWeatherAlert);
       }
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
       const payload = res.data || {};
       const updatedEvents = Array.isArray(payload.plannerEvents) ? payload.plannerEvents : payload;
       const nextWeatherAlert = payload.weatherAlert;
       if (nextWeatherAlert) {
         setWeatherAlert(nextWeatherAlert);
       }
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

  const handlePackSmart = (eventId) => {
    const event = plannerEvents.find(e => e.id === eventId);
    if (!event) return;

    const uniqueItemsMap = new Map();
    let totalOutfitDays = 0;
    
    // Group all items used in the event's itineraries
    event.itineraries?.forEach(itinerary => {
      if (itinerary.outfit && itinerary.outfit.items) {
        totalOutfitDays++;
        itinerary.outfit.items.forEach(item => {
          if (uniqueItemsMap.has(item.id)) {
            const data = uniqueItemsMap.get(item.id);
            data.count++;
          } else {
            uniqueItemsMap.set(item.id, { ...item, count: 1 });
          }
        });
      }
    });

    const groupedByType = {};
    const items = Array.from(uniqueItemsMap.values());
    
    items.forEach(item => {
      const typeStr = typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type;
      if (!groupedByType[typeStr]) groupedByType[typeStr] = [];
      groupedByType[typeStr].push(item);
    });

    const totalUnique = items.length;
    
    // Calculate luggage size
    let luggageEstimate = "Backpack (Minimalist)";
    if (totalUnique > 7 && totalUnique <= 15) luggageEstimate = "Carry-on (Standard)";
    else if (totalUnique > 15) luggageEstimate = "Checked Bag (Heavy)";

    // Analyze inefficiencies
    const inefficiencies = [];
    if (totalOutfitDays > 2) {
      items.forEach(item => {
        const typeStr = typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type;
        const typeLower = typeStr.toLowerCase();
        // If it's a bottom or shoes used only once, that's inefficient
        if ((typeLower.includes('bottom') || typeLower.includes('shoe') || typeLower.includes('sneaker') || typeLower.includes('boot') || typeLower.includes('pant') || typeLower.includes('jeans')) && item.count === 1) {
          inefficiencies.push(`You packed ${item.name || 'an item'} (${typeStr}) but only wear it once.`);
        }
      });
    }

    setPackSmartData({
      event,
      groupedByType,
      totalUnique,
      luggageEstimate,
      inefficiencies
    });
    setPackedItems([]); // reset checklist
    setPackSmartModal(true);
  };

  const onGenerateEventOutfits = async (plannerEventId) => {
    if (!confirm('This will generate AI outfits for each day of the event. Continue?')) {
      return;
    }
    setGeneratingModal(true);
    setGeneratingProgress({ status: 'Generating...', current: 0, total: 0 });
    try {
      const res = await plannerEventsApi.generateOutfits(plannerEventId, { userId });
      const alertPayload = plannerEventsApi.extractGenerateOutfitsWeatherAlert(res);
      if (alertPayload) {
        setWeatherAlert(alertPayload);
      }
      setGeneratingProgress({ 
        status: 'Done!', 
        current: res.data.outfitsCreated, 
        total: res.data.daysProcessed 
      });
      
      // Update local state immediately
      if (selectedPlannerEvent && selectedPlannerEvent.id === plannerEventId) {
        const res2 = await plannerEventsApi.getByUser(userId);
        const payload2 = res2.data || {};
        const events2 = Array.isArray(payload2.plannerEvents) ? payload2.plannerEvents : payload2;
        const updatedEvent = events2?.find(e => e.id === plannerEventId);
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

  const openPlannerForDate = useCallback((day) => {
    if (!day?.primaryEvent) {
      setView('planner');
      return;
    }

    setSelectedPlannerEvent(day.primaryEvent);
    setPlannerEventTab(day.primaryEvent.status === 'Archived' ? 'archived' : 'active');
    setSelectedDayIndex(getDayOffset(day.primaryEvent.startDate, day.date));
    fetchEventForecast(day.primaryEvent);
    setView('planner');
  }, [fetchEventForecast]);

  const openPlannerForToday = useCallback(() => {
    const todayTarget = upcomingWeekDays[0];
    if (todayTarget?.primaryEvent) {
      openPlannerForDate(todayTarget);
      return;
    }

    setView('planner');
  }, [openPlannerForDate, upcomingWeekDays]);

  const handleQuickAction = useCallback((action) => {
    switch (action) {
      case 'add-item':
        fileInputRef.current?.click();
        break;
      case 'create-outfit':
        setView('outfits');
        setCustomOutfitModal(true);
        break;
      case 'plan-today':
        openPlannerForToday();
        break;
      case 'generate-today':
        onGenerate();
        break;
      case 'open-planner':
        setView('planner');
        break;
      default:
        break;
    }
  }, [onGenerate, openPlannerForToday]);

  return (
    <div className="desktop-wrapper">
      <input 
        type="file" 
        multiple 
        ref={fileInputRef} 
        onChange={handleFileChange} 
        style={{ display: 'none' }} 
        accept="image/*"
      />
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
          <button 
            className="theme-toggle-btn" 
            style={{ fontSize: '0.6rem', color: 'var(--fg-muted)', background: 'var(--bg-subtle)' }}
            onClick={handleTestAlert}
          >
            Test Weather Alert
          </button>
          <button className="logout-btn" onClick={onLogout}>logout</button>
        </div>
      </aside>

      <main className="stage">
        <div className="centered-content">
          <h2 className="soft-title">
            {view === 'dashboard' ? 'dashboard' : view === 'clothes' ? 'your wardrobe' : view === 'outfits' ? 'generated outfits' : view === 'planner' ? 'outfit planner' : 'wardrobe insights'}
          </h2>

          {view === 'dashboard' && weatherAlert && (
            <div style={{ marginBottom: '20px' }}>
              <WeatherAlertNotice
                alert={weatherAlert}
                locationLabel={
                  weatherAlert?.plannerEventId && plannerEvents.find(e => e.id === weatherAlert.plannerEventId)?.location
                }
                onGenerateAlternative={() => {
                  if (weatherAlert?.plannerEventId) {
                    const event = plannerEvents.find(e => e.id === weatherAlert.plannerEventId);
                    if (event && weatherAlert.eventDate) {
                      // Need to find the itinerary ID for this specific date
                      const alertDate = new Date(weatherAlert.eventDate).toDateString();
                      const itinerary = event.itineraries.find(i => new Date(i.date).toDateString() === alertDate);
                      if (itinerary) {
                        onRegenerateItinerary(event.id, itinerary.id);
                        setWeatherAlert(null); // Dismiss alert after action
                        return;
                      }
                    }
                    onGenerateEventOutfits(weatherAlert.plannerEventId);
                  }
                }}
                onDismiss={() => setWeatherAlert(null)}
              />
            </div>
          )}

          {view === 'dashboard' ? (
            <div className="dashboard-layout dashboard-layout-v2">
              <section className="dashboard-hero-card">
                <div className="hero-main">
                  <div className="hero-chip">{greetingLabel}</div>
                  <h3 className="hero-title">{firstName}, here is your daily briefing.</h3>
                  <p className="hero-subtitle">
                    {todaysEventSummary.totalEvents === 0
                      ? 'No events planned today. Generate a look and schedule your next occasion.'
                      : `${todaysEventSummary.totalEvents} event${todaysEventSummary.totalEvents > 1 ? 's' : ''} today, ${todaysEventSummary.plannedCount} outfit${todaysEventSummary.plannedCount !== 1 ? 's' : ''} ready.`}
                  </p>

                  <div className="hero-metrics-grid">
                    <div className="hero-metric-card">
                      <span>today's events</span>
                      <strong>{todaysEventSummary.totalEvents}</strong>
                    </div>
                    <div className="hero-metric-card">
                      <span>ready outfits</span>
                      <strong>{todaysEventSummary.plannedCount}</strong>
                    </div>
                    <div className="hero-metric-card">
                      <span>need planning</span>
                      <strong>{todaysEventSummary.missingCount}</strong>
                    </div>
                    <div className="hero-metric-card">
                      <span>week readiness</span>
                      <strong>{weekDaysWithEvents.length === 0 ? 'n/a' : `${weekReadyDays}/${weekDaysWithEvents.length}`}</strong>
                    </div>
                  </div>

                  <div className="hero-actions-row">
                    <button className="hero-primary-action" onClick={() => handleQuickAction('generate-today')} disabled={loading || clothes.length === 0}>
                      Generate today&apos;s outfit
                    </button>
                    <button className="hero-secondary-action" onClick={openPlannerForToday}>
                      Plan missing looks
                    </button>
                  </div>
                </div>

                <div className="hero-side-panel">
                  <button
                    className="hero-location-button"
                    onClick={() => {
                      setSearchTerm('');
                      setCityModal(true);
                    }}
                  >
                    {city}
                  </button>
                  <div className="hero-weather-temp">{weatherInfo ? `${Math.round(weatherInfo.temperature)}°C` : '--'}</div>
                  <div className="hero-weather-condition">{weatherInfo?.condition || 'weather updating...'}</div>
                  <div className="hero-weather-meta">season tip: {weatherInfo?.seasonSuggestion || 'n/a'}</div>
                  <div className="hero-readiness-block">
                    <span>today readiness</span>
                    <strong>{todaysReadinessPercent}%</strong>
                    <div className="hero-progress-track">
                      <div className="hero-progress-fill" style={{ width: `${todaysReadinessPercent}%` }} />
                    </div>
                  </div>
                </div>
              </section>

              <section className="dashboard-quick-actions-card">
                <div className="dashboard-section-header">
                  <h3>Quick actions</h3>
                  <span>one-click workflows</span>
                </div>
                <div className="quick-actions-grid">
                  {quickActions.map((action) => (
                    <button
                      key={action.id}
                      type="button"
                      className="quick-action-button"
                      onClick={() => handleQuickAction(action.id)}
                      disabled={action.disabled}
                    >
                      <strong>{action.label}</strong>
                      <span>{action.hint}</span>
                    </button>
                  ))}
                </div>
              </section>

              <section className="dashboard-week-strip-card">
                <div className="dashboard-section-header">
                  <h3>Upcoming 7 days</h3>
                  <span>tap any day to jump into planner</span>
                </div>
                <div className="week-strip-grid">
                  {upcomingWeekDays.map((day) => (
                    <div
                      key={day.dayKey}
                      className={`week-day-card ${day.isToday ? 'today' : ''} ${day.status}`}
                      onClick={() => setPreviewDay(day)}
                      style={{ cursor: 'pointer', transition: 'all 0.2s', display: 'flex', flexDirection: 'column' }}
                    >
                      <div className="week-day-top">
                        <span>{day.weekdayLabel}</span>
                        {day.isToday && <small>today</small>}
                      </div>
                      <strong>{day.dayLabel}</strong>
                      <p className="week-day-weather">
                        {day.weather?.temperature !== undefined
                          ? `${Math.round(day.weather.temperature)}°C • ${day.weather.condition}`
                          : 'forecast pending'}
                      </p>
                      <p className="week-day-event-name">
                        {day.primaryEvent
                          ? `${day.primaryEvent.name}${day.totalEvents > 1 ? ` +${day.totalEvents - 1}` : ''}`
                          : 'No planned events'}
                      </p>
                      
                      {day.primaryItinerary?.outfit && (
                        <div style={{ display: 'flex', gap: '4px', overflowX: 'auto', margin: '8px 0', justifyContent: 'center' }}>
                          {day.primaryItinerary.outfit.items?.slice(0, 3).map(item => (
                            <img 
                              key={item.id} 
                              src={item.processedImageUrl} 
                              alt={item.name} 
                              style={{ width: '28px', height: '28px', borderRadius: '4px', border: '1px solid var(--border-subtle)', objectFit: 'cover', flexShrink: 0 }} 
                            />
                          ))}
                        </div>
                      )}

                      <span className="week-day-status" style={{ marginTop: 'auto' }}>
                        {day.totalEvents === 0
                          ? 'Free day'
                          : day.status === 'planned'
                            ? `Ready ${day.plannedCount}/${day.totalEvents}`
                            : `Needs plan ${day.plannedCount}/${day.totalEvents}`}
                      </span>
                    </div>
                  ))}
                </div>
              </section>

              <section className="dashboard-weather-compact">
                <div className="dashboard-section-header">
                  <h3>Weather focus</h3>
                  <span>quick context for generation</span>
                </div>
                <div className="weather-compact-content">
                  <button
                    className="weather-compact-location"
                    onClick={() => {
                      setSearchTerm('');
                      setCityModal(true);
                    }}
                  >
                    {city}
                  </button>
                  <div className="weather-compact-info">
                    <strong>{weatherInfo ? `${Math.round(weatherInfo.temperature)}°C` : '--'}</strong>
                    <span>{weatherInfo?.condition || 'condition unavailable'}</span>
                    <small>season suggestion: {weatherInfo?.seasonSuggestion || 'n/a'}</small>
                  </div>
                  <button className="hero-primary-action compact" onClick={() => handleQuickAction('generate-today')} disabled={loading || clothes.length === 0}>
                    Generate now
                  </button>
                </div>
              </section>

              {/* Sections Removed as requested */}
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
            <div className="outfits-view-container">
              <div className="outfits-header-bar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <div className="outfits-filters" style={{ display: 'flex', gap: '10px' }}>
                  <button 
                    className={`nav-btn ${outfitFilter === 'all' ? 'active' : ''}`} 
                    onClick={() => setOutfitFilter('all')}
                  >
                    All Outfits
                  </button>
                  <button 
                    className={`nav-btn ${outfitFilter === 'favorites' ? 'active' : ''}`} 
                    onClick={() => setOutfitFilter('favorites')}
                  >
                    Favorites
                  </button>
                </div>
                <button 
                  className="gen-btn"
                  onClick={() => setCustomOutfitModal(true)}
                  style={{ padding: '10px 24px', fontSize: '0.85rem', background: 'var(--accent)', color: 'var(--accent-fg)', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer', boxShadow: '0 4px 12px rgba(var(--accent-rgb), 0.3)' }}
                >
                  + CREATE OUTFIT
                </button>
              </div>
              <div className="outfits-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '20px' }}>
              {outfits.filter(o => outfitFilter === 'all' || o.isFavorite).map(o => (
                <div key={o.id} className="outfit-card" style={{ background: 'var(--card-bg)', borderRadius: '16px', border: '1px solid var(--border)', overflow: 'hidden', display: 'flex', flexDirection: 'column', boxShadow: '0 4px 6px rgba(0,0,0,0.05)', transition: 'transform 0.2s', '&:hover': { transform: 'translateY(-4px)' } }}>
                  <div className="outfit-card-header" style={{ padding: '16px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <div>
                      <h3 style={{ margin: '0 0 4px 0', fontSize: '1.1rem', color: 'var(--fg)' }}>{o.name}</h3>
                      <span style={{ fontSize: '0.75rem', color: 'var(--fg-muted)' }}>{new Date(o.createdAt).toLocaleDateString()} {o.isAiGenerated && '• AI Generated'}</span>
                      {o.tags && o.tags.length > 0 && (
                        <div style={{ display: 'flex', gap: '4px', marginTop: '6px', flexWrap: 'wrap' }}>
                          {o.tags.map(tag => (
                            <span key={tag} style={{ background: 'var(--bg-subtle)', color: 'var(--fg)', padding: '2px 6px', borderRadius: '4px', fontSize: '0.6rem', border: '1px solid var(--border-subtle)' }}>
                              {tag}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                    <button 
                      onClick={() => onToggleFavorite(o)} 
                      style={{ background: 'transparent', border: 'none', cursor: 'pointer', padding: '4px', display: 'flex', alignItems: 'center', justifyContent: 'center', transition: 'transform 0.2s', color: o.isFavorite ? 'var(--danger)' : 'var(--fg-muted)' }}
                      title={o.isFavorite ? "Remove from favorites" : "Add to favorites"}
                    >
                      <svg width="22" height="22" viewBox="0 0 24 24" fill={o.isFavorite ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinelinejoin="round">
                        <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                      </svg>
                    </button>
                  </div>
                  <div className="outfit-card-items" style={{ padding: '16px', display: 'flex', gap: '10px', overflowX: 'auto', flex: 1, alignItems: 'center', background: 'var(--bg)' }}>
                    {o.items && o.items.map(i => (
                      <div key={i.id} onClick={() => setSelectedItem(i)} style={{ flexShrink: 0, width: '70px', height: '70px', borderRadius: '12px', background: 'var(--bg-raised)', cursor: 'pointer', overflow: 'hidden', border: '1px solid var(--border-subtle)' }}>
                        <img src={i.processedImageUrl} alt="" title={i.name} style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
                      </div>
                    ))}
                  </div>
                  <div className="outfit-card-actions" style={{ padding: '12px 16px', display: 'flex', gap: '8px', background: 'var(--card-bg)', borderTop: '1px solid var(--border-subtle)' }}>
                    <button onClick={() => onWearOutfit(o.id)} style={{ flex: 1, background: 'var(--accent)', color: 'var(--accent-fg)', border: 'none', padding: '8px 0', borderRadius: '8px', fontSize: '0.75rem', fontWeight: 'bold', cursor: 'pointer' }}>WEAR</button>
                    <button onClick={() => { setPlanData({ outfitId: o.id, plannerEventId: '', selectedDayIndex: null, moment: '' }); setPlanModal(true); }} style={{ flex: 1, background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border)', padding: '8px 0', borderRadius: '8px', fontSize: '0.75rem', fontWeight: 'bold', cursor: 'pointer' }}>PLAN</button>
                    <button className="edit-mini-btn" onClick={() => { setEditData({ id: o.id, name: o.name, itemIds: o.items?.map(i => i.id) || [] }); setEditModal(true); }} style={{ padding: '8px', background: 'var(--bg-raised)', color: 'var(--fg)', border: '1px solid var(--border)', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' }} title="Edit">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                    </button>
                    <button onClick={() => onDelete('outfit', o.id)} style={{ padding: '8px', background: 'var(--danger-bg)', color: 'var(--danger)', border: '1px solid var(--danger-border, var(--danger))', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' }} title="Delete">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                    </button>
                  </div>
                </div>
              ))}
              </div>
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
                          onClick={() => handlePackSmart(selectedPlannerEvent.id)}
                          style={{ background: 'var(--accent)', color: 'var(--accent-fg)' }}
                        >
                          PACK SMART 🎒
                        </button>
                        <button 
                          className="gen-btn"
                          onClick={() => onGenerateEventOutfits(selectedPlannerEvent.id)}
                        >
                          GENERATE OUTFITS
                        </button>
                        <button 
                          className="small-action-btn"
                          onClick={() => {
                            setEditEventData({
                              id: selectedPlannerEvent.id,
                              name: selectedPlannerEvent.name,
                              type: selectedPlannerEvent.type,
                              location: selectedPlannerEvent.location,
                              startDate: selectedPlannerEvent.startDate.split('T')[0],
                              endDate: selectedPlannerEvent.endDate.split('T')[0],
                              preferredStyles: selectedPlannerEvent.preferredStyles || []
                            });
                            setEditEventModal(true);
                          }}
                        >
                          EDIT EVENT
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
                          onClick={() => openOutfitEditingModal(selectedPlannerEvent.id, null, plannerDays[selectedDayIndex], selectedDayIndex, 'plan')}
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

              {/* Advanced Stats Section */}
              <StatsSection userId={userId} />

            </div>
          )}

      <UploadModal 
        isOpen={uploadModal} 
        onClose={() => setUploadModal(false)} 
        uploadData={uploadData} 
        setUploadData={setUploadData} 
        onUpload={onUpload} 
        loading={loading} 
      />

      <EditOutfitModal 
        isOpen={editModal} 
        onClose={() => setEditModal(false)} 
        editData={editData} 
        setEditData={setEditData} 
        clothes={clothes} 
        onEditSave={onEditSave} 
        loading={loading} 
      />

      <ItemInspectModal 
        isOpen={!!selectedItem} 
        onClose={() => { setSelectedItem(null); setEditItemMode(false); }} 
        selectedItem={selectedItem} 
        editItemMode={editItemMode} 
        setEditItemMode={setEditItemMode} 
        editItemData={editItemData} 
        setEditItemData={setEditItemData} 
        onUpdateItem={onUpdateItem} 
        onGenerate={onGenerate} 
        loading={loading} 
      />

      <StyleSelectionModal 
        isOpen={styleSelectionModal} 
        onClose={() => setStyleSelectionModal(false)} 
        executeGeneration={executeGeneration} 
      />

      <AiSuggestionModal 
        isOpen={aiModal} 
        onClose={() => setAiModal(false)} 
        aiData={aiData} 
        setAiData={setAiData} 
        onSaveAiOutfit={onSaveAiOutfit} 
        loading={loading} 
      />

      <ValidationModal 
        isOpen={validationModal} 
        onClose={() => setValidationModal(false)} 
        renderValidationStep={renderValidationStep} 
      />

      <CitySelectionModal 
        isOpen={cityModal} 
        onClose={() => setCityModal(false)} 
        searchTerm={searchTerm} 
        setSearchTerm={setSearchTerm} 
        citySuggestions={citySuggestions} 
        handleCityChange={handleCityChange} 
      />

      <PlanOutfitModal 
        isOpen={planModal} 
        onClose={() => setPlanModal(false)} 
        planData={planData} 
        setPlanData={setPlanData} 
        plannerEvents={plannerEvents} 
        currentEventDays={currentEventDays} 
        onPlanOutfit={onPlanOutfit} 
        loading={loading} 
      />

      <EditItineraryModal 
        isOpen={editItineraryModal} 
        onClose={() => { setEditItineraryModal(false); setEditItineraryData({ plannerEventId: "", itineraryId: "", outfitId: "", date: "", moment: "" }); }} 
        editItineraryData={editItineraryData} 
        setEditItineraryData={setEditItineraryData} 
        outfits={outfits} 
        onUpdateItinerary={onUpdateItinerary} 
        loading={loading} 
      />

      {/* CREATE EVENT MODAL - 3-Step Wizard */}
      <CreateEventModal 
        isOpen={createEventModal} 
        onClose={() => { 
          setCreateEventModal(false); 
          setWizardStep(0); 
          setWizardPreview(null); 
          setCreateEventData({ name: "", type: "Vacation", location: "", startDate: "", endDate: "", preferredStyles: [] }); 
        }} 
        wizardStep={wizardStep} 
        setWizardStep={setWizardStep} 
        wizardPreview={wizardPreview} 
        setWizardPreview={setWizardPreview} 
        createEventData={createEventData} 
        setCreateEventData={setCreateEventData} 
        eventLocationSearch={eventLocationSearch} 
        setEventLocationSearch={setEventLocationSearch} 
        eventLocationSuggestions={eventLocationSuggestions} 
        setEventLocationSuggestions={setEventLocationSuggestions} 
        onPreviewEvent={onPreviewEvent} 
        onCreatePlannerEvent={onCreatePlannerEvent} 
        wizardLoading={wizardLoading} 
        loading={loading} 
      />
      {/* EDIT EVENT MODAL */}
      <EditEventModal 
        isOpen={editEventModal} 
        onClose={() => setEditEventModal(false)} 
        editEventData={editEventData} 
        setEditEventData={setEditEventData} 
        onUpdatePlannerEvent={onUpdatePlannerEvent} 
        loading={loading} 
      />

      {/* CUSTOM OUTFIT MODAL */}
      <CustomOutfitModal 
        isOpen={customOutfitModal} 
        onClose={() => setCustomOutfitModal(false)} 
        customOutfitData={customOutfitData} 
        setCustomOutfitData={setCustomOutfitData} 
        customOutfitTab={customOutfitTab} 
        setCustomOutfitTab={setCustomOutfitTab} 
        clothes={clothes} 
        onSaveCustomOutfit={onSaveCustomOutfit} 
        loading={loading} 
      />

      {/* GENERATING OUTFITS MODAL */}
      <GeneratingModal 
        isOpen={generatingModal} 
        onClose={() => setGeneratingModal(false)} 
        generatingProgress={generatingProgress} 
      />

      <DayPreviewModal 
        isOpen={!!previewDay} 
        onClose={() => setPreviewDay(null)} 
        previewDay={previewDay} 
        onWearOutfit={onWearOutfit} 
        setSelectedPlannerEvent={setSelectedPlannerEvent} 
        setSelectedDayIndex={setSelectedDayIndex} 
        getDayOffset={getDayOffset} 
        setView={setView} 
      />
      {/* PACK SMART MODAL */}
      <PackSmartModal 
        isOpen={packSmartModal} 
        onClose={() => setPackSmartModal(false)} 
        packSmartData={packSmartData} 
        packedItems={packedItems} 
        setPackedItems={setPackedItems} 
      />

      {/* OUTFIT EDITING MODAL */}
      <OutfitEditingModal
        isOpen={outfitEditingModal}
        onClose={() => setOutfitEditingModal(false)}
        onSave={onSaveOutfitEdit}
        clothes={clothes}
        outfits={outfits}
        currentOutfit={selectedDayItinerary?.outfit}
        dayInfo={outfitEditingData.dayIndex !== null ? plannerDays[outfitEditingData.dayIndex] : null}
        loading={loading}
        mode={outfitEditingData.mode}
        initialMoment={outfitEditingData.moment || ''}
      />
      </div>
    </main>
    </div>
  );
};

export default DashboardPage;

