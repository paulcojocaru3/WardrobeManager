import { useCallback, useMemo, useState, useEffect, useRef } from 'react';
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
import SettingsSection from '../components/SettingsSection';
import NotificationBell from '../components/NotificationBell';
import { authApi, clothingApi, geoApi, outfitsApi, plannerEventsApi, statsApi } from '../services/wardrobeApi';
import { COLORS, CLOTHING_TYPES, SEASONS, USAGES, EVENT_MOMENTS } from '../constants/wardrobe';
import { getErrorMessage } from '../utils/errors';
import { toCsv, toTypeIndex } from '../utils/wardrobeTransforms';
import { useTheme } from '../contexts/ThemeContext';
import { useNotifications } from '../contexts/NotificationContext';

const DAY_IN_MS = 24 * 60 * 60 * 1000;
const OOTD_CACHE_VERSION = 'v3';

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

const IC = {
  sparkles: <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3l1.7 4.3L18 9l-4.3 1.7L12 15l-1.7-4.3L6 9l4.3-1.7Z"/><path d="M19 14l.9 2.1L22 17l-2.1.9L19 20l-.9-2.1L16 17l2.1-.9Z"/><path d="M5 16l.6 1.4L7 18l-1.4.6L5 20l-.6-1.4L3 18l1.4-.6Z"/></svg>,
  hanger:   <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M12 7a2 2 0 1 1 2-2"/><path d="M12 7v2.5L3 16h18l-9-6.5"/></svg>,
  layers:   <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3 2 8l10 5 10-5z"/><path d="M2 13l10 5 10-5"/><path d="M2 18l10 5 10-5"/></svg>,
  calendar: <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/></svg>,
  chart:    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/></svg>,
  plus:     <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"><path d="M12 5v14M5 12h14"/></svg>,
  sun:      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>,
  moon:     <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>,
  logout:   <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>,
  settings: <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>,
};

const DashboardPage = ({ user, onLogout, onUserUpdate }) => {
  const { isDarkMode, toggleTheme } = useTheme();
  const { pushToast } = useNotifications();
  const [genericForecast, setGenericForecast] = useState([]);
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [outfitFilter, setOutfitFilter] = useState('all'); // 'all', 'favorites'
  const [outfitView, setOutfitView] = useState('grid'); // 'grid', 'list'
  const [wardrobeSearch, setWardrobeSearch] = useState('');
  const [wardrobeTypeFilter, setWardrobeTypeFilter] = useState('ALL');
  const [wardrobeTagFilter, setWardrobeTagFilter] = useState(null);
  const [plannerEvents, setPlannerEvents] = useState([]);
  const [usageRate, setUsageRate] = useState(0);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('generate');
  const [previewDay, setPreviewDay] = useState(null);
  const [selectedItem, setSelectedItem] = useState(null);
  const [editItemMode, setEditItemMode] = useState(false);
  const [editItemData, setEditItemData] = useState(null);
  const [subtypeOptions, setSubtypeOptions] = useState({});
  
  const [uploadModal, setUploadModal] = useState(false);
  const [uploadData, setUploadData] = useState([]);
  
  const [validationModal, setValidationModal] = useState(false);
  const [validationData, setValidationData] = useState(null);
  const [validationQueue, setValidationQueue] = useState([]);
  const [originalPredictions, setOriginalPredictions] = useState(null);
  const [currentStep, setCurrentStep] = useState(0); 
  const [validationSearchTerm, setValidationSearchTerm] = useState('');
  
  const [editModal, setEditModal] = useState(false);
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [], tags: [] });

  const [planModal, setPlanModal] = useState(false);
  const [planData, setPlanData] = useState({ outfitId: null, plannerEventId: '', selectedDayIndex: null, moment: '' });

  const [city, setCity] = useState(localStorage.getItem('userCity') || 'Detecting...');
  const [cityModal, setCityModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [weatherInfo, setWeatherInfo] = useState(null);
  const [citySuggestions, setCitySuggestions] = useState([]);
  const [styleSelectionModal, setStyleSelectionModal] = useState(false);
  const [generationContext, setGenerationContext] = useState(null);
  // track rediscover generation state.
  const [preferUnused, setPreferUnused] = useState(false);

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

  // Nearest current/upcoming planner event — drives the "Next up" dashboard module.
  const nextUpEvent = useMemo(() => {
    const today = toDayStart(new Date());
    const upcoming = plannerEvents
      .filter((e) => toDayEnd(e.endDate) >= today)
      .sort((a, b) => new Date(a.startDate) - new Date(b.startDate));
    const event = upcoming[0];
    if (!event) return null;

    const start = toDayStart(event.startDate);
    const daysUntil = Math.max(0, Math.round((start.getTime() - today.getTime()) / DAY_IN_MS));
    const totalDays = getEventDays(event).length;
    const planned = (event.itineraries || []).filter((i) => i.outfitId || i.outfit).length;
    const forecast = (eventForecasts[event.id] || [])[0] || null;
    return { event, daysUntil, totalDays, planned, needsPlan: planned < totalDays, forecast };
  }, [plannerEvents, eventForecasts, getEventDays]);



  const [editItineraryModal, setEditItineraryModal] = useState(false);
const [editItineraryData, setEditItineraryData] = useState({
    plannerEventId: '',
    itineraryId: '',
    outfitId: '',
    date: '',
    moment: ''
  });

  const [createEventModal, setCreateEventModal] = useState(false);
  const [createEventData, setCreateEventData] = useState({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [], reuseAfterDays: 3 });
  const [editEventModal, setEditEventModal] = useState(false);
  const [editEventData, setEditEventData] = useState({ id: '', name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [], reuseAfterDays: null });
  const [eventLocationSearch, setEventLocationSearch] = useState('');
  const [eventLocationSuggestions, setEventLocationSuggestions] = useState([]);

  const [generatingModal, setGeneratingModal] = useState(false);
  const [generatingProgress, setGeneratingProgress] = useState(null);

  const [aiModal, setAiModal] = useState(false);
  const [aiData, setAiData] = useState(null);
  // store generated outfit notes.
  const [aiStylingNotes, setAiStylingNotes] = useState([]);
  const [notesLoading, setNotesLoading] = useState(false);
  const [aiInsight, setAiInsight] = useState(null);

  // cache the daily outfit state.
  const [ootd, setOotd] = useState(null);
  const [ootdLoading, setOotdLoading] = useState(false);
  const [ootdOccasion] = useState('casual');
  // store daily outfit insight.
  const [ootdInsight, setOotdInsight] = useState(null);
  const [ootdInsightLoading, setOotdInsightLoading] = useState(false);
  // surface forgotten items for rediscover.
  const [forgottenItems, setForgottenItems] = useState([]);
  
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
  const useGemmaStylistForOutfits = user?.useGemmaStylistForOutfits ?? user?.UseGemmaStylistForOutfits ?? false;
  const defaultReuseAfterDays = user?.defaultReuseAfterDays !== undefined
    ? user.defaultReuseAfterDays
    : (user?.DefaultReuseAfterDays !== undefined ? user.DefaultReuseAfterDays : 3);
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

  // Long date for the editorial eyebrow at the top of the Generate view.
  const todayLabel = new Date().toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' });

  const handleSaveProfile = async (payload) => {
    const res = await authApi.updateUser(userId, payload);
    onUserUpdate(res.data);
  };

  // Low-sensitivity preferences (favorite colors, city, theme) — no password needed.
  const handleSavePreferences = useCallback(async (payload) => {
    const res = await authApi.updatePreferences(userId, payload);
    onUserUpdate(res.data);
    return res.data;
  }, [userId, onUserUpdate]);

  const handleDeleteAccount = async () => {
    await authApi.deleteUser(userId);
    onLogout();
  };

  // Toggle theme locally and persist the choice to the account.
  const handleToggleTheme = useCallback(() => {
    const next = isDarkMode ? 'light' : 'dark';
    toggleTheme();
    authApi.updatePreferences(userId, { themePreference: next })
      .then((res) => onUserUpdate(res.data))
      .catch(() => { /* local toggle still applies */ });
  }, [isDarkMode, toggleTheme, userId, onUserUpdate]);

  // Apply the account's saved theme once on mount (account wins over local cache).
  const themeAppliedRef = useRef(false);
  useEffect(() => {
    if (themeAppliedRef.current) return;
    themeAppliedRef.current = true;
    const pref = user?.themePreference || user?.ThemePreference;
    if (pref === 'dark' && !isDarkMode) toggleTheme();
    if (pref === 'light' && isDarkMode) toggleTheme();
  }, [user, isDarkMode, toggleTheme]);

  const aiOutfitCount = useMemo(() => outfits.filter((outfit) => outfit.isAiGenerated).length, [outfits]);


  const wardrobeTags = useMemo(() => {
    const tagSet = new Set();
    clothes.forEach(item => {
      if (item.usage) {
        item.usage.split(',').forEach(t => {
          const trimmed = t.trim();
          if (trimmed) tagSet.add(trimmed);
        });
      }
    });
    return Array.from(tagSet).sort();
  }, [clothes]);

  const filteredClothes = useMemo(() => {
    return clothes.filter(item => {
      if (wardrobeTypeFilter !== 'ALL') {
        const typeName = typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type;
        if (typeName !== wardrobeTypeFilter) return false;
      }
      if (wardrobeSearch) {
        const name = (item.name || '').toLowerCase();
        if (!name.includes(wardrobeSearch.toLowerCase())) return false;
      }
      if (wardrobeTagFilter) {
        const usageTags = (item.usage || '').split(',').map(t => t.trim());
        if (!usageTags.includes(wardrobeTagFilter)) return false;
      }
      return true;
    });
  }, [clothes, wardrobeTypeFilter, wardrobeSearch, wardrobeTagFilter]);


  useEffect(() => {
    const detectLocation = async () => {
      // Account preference wins, then the local cache, then geo-detection.
      const accountCity = user?.preferredCity || user?.PreferredCity;
      if (accountCity) {
        setCity(accountCity);
        localStorage.setItem('userCity', accountCity);
        return;
      }

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
    // Runs once on mount; account city is read from the user captured at login.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCityChange = useCallback((newCity) => {
    setCity(newCity);
    localStorage.setItem('userCity', newCity);
    // Sync to the account so it follows the user across devices (best-effort).
    authApi.updatePreferences(userId, { preferredCity: newCity })
      .then((res) => onUserUpdate(res.data))
      .catch(() => { /* keep local change even if sync fails */ });
  }, [userId, onUserUpdate]);

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
       setPlannerEvents(Array.isArray(events) ? events : []);
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

  // Forgotten pieces (not worn recently) for the "rediscover" nudge on the Generate view.
  const fetchForgottenItems = useCallback(async () => {
    if (!userId) return;
    try {
      const res = await statsApi.getWearStats(userId, { range: 'all-time' });
      setForgottenItems(Array.isArray(res.data?.unwornRecently) ? res.data.unwornRecently : []);
    } catch (e) {
      console.error('Forgotten items error:', e);
    }
  }, [userId]);

  // Sub-type vocabulary (grouped by type) for the edit dropdown — static metadata, fetched once.
  const fetchSubtypes = useCallback(async () => {
    try {
      const res = await clothingApi.getSubtypes();
      setSubtypeOptions(res.data || {});
    } catch (e) {
      console.error('Subtypes error:', e);
    }
  }, []);

  const refresh = useCallback(() => {
    if (!userId) return;

    fetchClothes();
    fetchOutfits();
    fetchPlannerEvents();
    fetchArchivedPlannerEvents();
    fetchWeather();
    fetchUsageRate();
    fetchForgottenItems();
    fetchSubtypes();
  }, [fetchClothes, fetchOutfits, fetchPlannerEvents, fetchArchivedPlannerEvents, fetchWeather, fetchUsageRate, fetchForgottenItems, fetchSubtypes, userId]);

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
    // The user picks a style next; the optional occasion chip flavors the styling notes.
    setStyleSelectionModal(true);
  };

  // fetch notes without blocking the modal.
  const fetchStylingNotes = async (outfit, style) => {
    const itemIds = (outfit?.selectedItems || []).map(i => i.id);
    if (itemIds.length === 0) return;
    setNotesLoading(true);
    setAiStylingNotes([]);
    try {
      const { data } = await outfitsApi.getStylingNotes({
        itemIds,
        style: style || null,
        occasion: null,
        city,
        tradeoffs: outfit?.warnings || [],
      });
      setAiStylingNotes(data.notes || []);
    } catch {
      setAiStylingNotes([]);
    } finally {
      setNotesLoading(false);
    }
  };

  // build around a rarely worn server-selected seed.
  const onRediscover = () => {
    setSelectedItem(null);
    setGenerationContext('rediscover');
    setStyleSelectionModal(true);
  };

  const executeGeneration = async (style, overrideCity = null) => {
    setStyleSelectionModal(false);
    setLoading(true);
    if (useGemmaStylistForOutfits) {
      setGeneratingProgress({
        mode: 'stylist',
        status: 'Gemma3 is styling your outfit',
        detail: 'FashionCLIP is casting candidates, then Gemma3 chooses the final look before it opens.',
        current: 1,
        total: 3,
      });
      setGeneratingModal(true);
    }
    const effectiveCity = overrideCity || city;
    const rediscover = generationContext === 'rediscover';

    let startItem = selectedItem;
    if (generationContext === 'today') {
      const candidates = style
        ? clothes.filter(c => c.usage?.toLowerCase().includes(style.toLowerCase()))
        : clothes;
      startItem = candidates.length > 0
        ? candidates[Math.floor(Math.random() * candidates.length)]
        : clothes[Math.floor(Math.random() * clothes.length)];
    }

    // let rediscover pick the seed on the server.
    if (!rediscover && !startItem) { setLoading(false); return; }

    try {
      const payload = {
        userId,
        threshold: 0.5,
        city: effectiveCity,
        style,
        season: weatherInfo?.seasonSuggestion,
        preferUnusedItems: preferUnused || rediscover,
      };
      if (rediscover) {
        payload.anchorOnUnused = true;
      } else {
        payload.startItemId = startItem.id;
      }

      const { data } = await outfitsApi.generateAi(payload);
      if (useGemmaStylistForOutfits) setGeneratingModal(false);
      setAiData(data);
      setAiInsight(null);
      setAiStylingNotes([]);
      setAiModal(true);
      fetchStylingNotes(data, style);
    } catch (err) {
      if (useGemmaStylistForOutfits) setGeneratingModal(false);
      handleApiAlert(err, 'Generation failed');
    }
    finally {
      setLoading(false);
      setSelectedItem(null);
      setPreferUnused(false);
      if (useGemmaStylistForOutfits) setGeneratingProgress(null);
    }
  };

  // keep one daily outfit stable until shuffle.
  const ootdRealCity = (city && city !== 'Detecting...') ? city : null;
  const weatherLocationLabel = ootdRealCity || 'Detecting location';
  const ootdCtxRef = useRef({ city: null, weatherInfo: null });
  ootdCtxRef.current = { city: ootdRealCity, weatherInfo };
  const ootdKeyRef = useRef(null);
  const ootdColorPreferenceKey = [
    ...(user?.favoriteColors || user?.FavoriteColors || []).map(color => `fav:${color.toLowerCase()}`),
    ...(user?.avoidColors || user?.AvoidColors || []).map(color => `avoid:${color.toLowerCase()}`),
  ].sort().join(',');
  // remember stylist refinements by generation.

  // fetch daily outfit insight for cache.
  const fetchOotdInsight = async (outfit, notesCity) => {
    const itemIds = (outfit?.selectedItems || []).map(i => i.id);
    if (itemIds.length === 0) return null;
    setOotdInsightLoading(true);
    setOotdInsight(null);
    try {
      const { data } = await outfitsApi.getOutfitInsight({
        itemIds,
        style: null,
        occasion: null,
        city: notesCity,
        tradeoffs: outfit?.warnings || [],
      });
      setOotdInsight(data);
      return data;
    } catch {
      setOotdInsight(null);
      return null;
    } finally {
      setOotdInsightLoading(false);
    }
  };

  const loadOutfitOfDay = useCallback(async (force = false) => {
    if (!userId || clothes.length === 0) return;
    const dateKey = new Date().toISOString().slice(0, 10);
    const key = `ootd_${OOTD_CACHE_VERSION}_${userId}_${dateKey}_${ootdOccasion}_${ootdColorPreferenceKey}`;
    const { city: ctxCity, weatherInfo: ctxWeather } = ootdCtxRef.current;

    // Already loaded (or loading) today's pick in this session — leave it alone unless forced.
    // This is what keeps the look stable when you switch between Generate and other tabs.
    if (!force && ootdKeyRef.current === key) return;

    if (!force) {
      const cached = localStorage.getItem(key);
      if (cached) {
        try {
          const entry = JSON.parse(cached);
          // New shape: { outfit, insight, ... }. Old shapes: { outfit, notes } or the raw outfit.
          const outfit = entry.outfit || entry;
          ootdKeyRef.current = key;
          setOotd(outfit);
          if (entry.insight) {
            setOotdInsight(entry.insight);
          } else {
            fetchOotdInsight(outfit, ctxCity); // legacy cache: backfill insight (no regenerate)
          }
          return;
        } catch { /* fall through to regenerate */ }
      }
    }

    // Claim the key up front so concurrent re-entries (fast tab switches) don't kick off a 2nd generation.
    ootdKeyRef.current = key;
    setOotdLoading(true);
    setOotdInsight(null);
    try {
      const payload = {
        userId,
        threshold: 0.5,
        season: ctxWeather?.seasonSuggestion,
        preferUnusedItems: true,
        anchorOnUnused: true,
        occasion: ootdOccasion,
        shuffle: force,
      };
      if (ctxCity) payload.city = ctxCity;

      const { data } = await outfitsApi.generateAi(payload);
      setOotd(data);

      // Persist the look immediately (before the slower LLM insight) so a reload/tab-switch right
      // after generation reads it from cache instead of generating a different outfit.
      const baseEntry = {
        outfit: data,
        insight: null,
        city: ctxCity,
        temp: ctxWeather ? Math.round(ctxWeather.temperature) : null,
        condition: ctxWeather?.condition || null,
      };
      try { localStorage.setItem(key, JSON.stringify(baseEntry)); } catch { /* storage full — skip cache */ }

      const insight = await fetchOotdInsight(data, ctxCity);
      try { localStorage.setItem(key, JSON.stringify({ ...baseEntry, insight })); } catch { /* skip */ }
    } catch {
      ootdKeyRef.current = null; // generation failed — allow a retry
      setOotd(null);
    } finally {
      setOotdLoading(false);
    }
  }, [userId, clothes.length, ootdOccasion, ootdColorPreferenceKey]);

  // Auto-load the daily pick when the user is on the Generate view and the wardrobe is ready.
  useEffect(() => {
    if (view === 'generate') loadOutfitOfDay(false);
  }, [view, loadOutfitOfDay]);


  // Open the daily pick in the full suggestion modal. The rich insight (headline + per-item notes +
  // weather advice) is already computed for the daily look, so show that instead of the flat notes.
  const openOutfitOfDay = () => {
    if (!ootd) return;
    setAiData(ootd);
    setAiInsight(ootdInsight || null);
    setAiStylingNotes([]);
    setAiModal(true);
    if (!ootdInsight) fetchStylingNotes(ootd, null); // fallback only if the insight didn't load
  };

  // Generate a weather-aware outfit anchored on a forgotten item and open it directly.
  const buildAroundForgotten = async (item) => {
    setLoading(true);
    try {
      const payload = {
        userId,
        threshold: 0.5,
        season: weatherInfo?.seasonSuggestion,
        preferUnusedItems: true,
        startItemId: item.id,
      };
      if (ootdRealCity) payload.city = ootdRealCity;

      const { data } = await outfitsApi.generateAi(payload);
      setAiData(data);
      setAiInsight(null);
      setAiStylingNotes([]);
      setAiModal(true);
      fetchStylingNotes(data, null);
    } catch (err) {
      handleApiAlert(err, 'Generation failed');
    } finally {
      setLoading(false);
    }
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

      // When the user enabled auto-blocking, reject near-duplicates outright instead of
      // routing them through validation; the rest continue normally.
      const blockDupes = user?.blockDuplicateUploads ?? user?.BlockDuplicateUploads ?? false;
      let toValidate = results;
      if (blockDupes) {
        const rejected = results.filter(r => r.possibleDuplicates?.length > 0);
        toValidate = results.filter(r => !(r.possibleDuplicates?.length > 0));
        if (rejected.length > 0) {
          const names = rejected.map(r => r.possibleDuplicates[0]?.name).filter(Boolean).join(', ');
          alert(`Skipped ${rejected.length} item(s) you already own (similar to: ${names}). Turn off "Block duplicate uploads" in Preferences to add them anyway.`);
        }
      }

      if (toValidate.length > 0) {
        setValidationQueue(toValidate);

        const firstItem = toValidate[0];
        setOriginalPredictions({
          type: firstItem.type,
          color: firstItem.color,
          gender: firstItem.gender,
          season: firstItem.season,
          usage: firstItem.usage
        });

        setValidationData({
          ...firstItem,
          color: firstItem.color ? [firstItem.color] : [],
          season: firstItem.season ? [firstItem.season] : [],
          usage: firstItem.usage ? [firstItem.usage] : []
        });

        setCurrentStep(0);
        setUploadModal(false);
        setValidationModal(true);
      } else {
        // Everything was blocked as a duplicate — just close the upload modal.
        setUploadModal(false);
        setUploadData([]);
      }
    } catch (err) {
      handleApiAlert(err, 'Processing failed');
    }
    finally { setLoading(false); }
  };

  const onConfirmStep = () => {
    setValidationSearchTerm('');
    if (currentStep < 3) {
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
        subType: validationData.subType,
        color: toCsv(validationData.color),
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
          color: nextItem.color ? [nextItem.color] : [],
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
      { label: 'COLOR', value: validationData.color, options: COLORS, field: 'color', isMulti: true, isSearchable: true, original: originalPredictions.color },
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

        {currentStep === 0 && validationData.possibleDuplicates?.length > 0 && (
          <div className="validation-dupe-notice" style={{ display: 'flex', gap: '12px', alignItems: 'center', padding: '12px 16px', marginBottom: '20px', background: 'var(--bg-subtle)', border: '1px solid var(--border-muted)', borderRadius: '14px' }}>
            <div style={{ display: 'flex', gap: '8px' }}>
              {validationData.possibleDuplicates.slice(0, 3).map(d => (
                <img key={d.id} src={d.imageUrl} alt={d.name} title={`${d.name} · ${Math.round((d.similarity ?? 0) * 100)}% match`} style={{ width: '44px', height: '44px', objectFit: 'cover', borderRadius: '8px', border: '1px solid var(--border-subtle)' }} />
              ))}
            </div>
            <div style={{ fontSize: '0.72rem', color: 'var(--fg-faint)', lineHeight: 1.4 }}>
              You may already own this — {Math.round((validationData.possibleDuplicates[0].similarity ?? 0) * 100)}% similar to{' '}
              <strong>{validationData.possibleDuplicates[0].name}</strong>. You can still add it — just checking.
            </div>
          </div>
        )}

        <div className="step-indicator" style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '30px' }}>
          {steps.map((_, i) => (
            <div key={i} style={{ width: '30px', height: '3px', background: i === currentStep ? 'var(--accent)' : (i < currentStep ? 'var(--border-muted)' : 'var(--border-subtle)'), borderRadius: '2px', transition: 'all 0.3s' }} />
          ))}
        </div>

        <div style={{ marginBottom: '30px', textAlign: 'left' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <span className="robotic-text" style={{ fontSize: '0.6rem', color: 'var(--fg-faint)' }}>STEP {currentStep + 1} OF {steps.length}: VERIFY {step.label}</span>
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
                    let next;
                    if (step.field === 'season' && opt === 'All Seasons') {
                      next = currentArray.includes(opt) ? [] : ['All Seasons']; // exclusive
                    } else if (step.field === 'season') {
                      next = currentArray.includes(opt)
                        ? currentArray.filter(i => i !== opt)
                        : [...currentArray.filter(i => i !== 'All Seasons'), opt];
                    } else {
                      next = currentArray.includes(opt) ? currentArray.filter(i => i !== opt) : [...currentArray, opt];
                    }
                    setValidationData({ ...validationData, [step.field]: next });
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
          <button className="gen-btn" onClick={onConfirmStep} disabled={loading} style={{ flex: 2, padding: '12px' }}>{loading ? 'SAVING...' : (currentStep === steps.length - 1 ? "COMPLETE & SAVE" : "CONTINUE")}</button>
        </div>
      </div>
    );
  };

  const onSaveAiOutfit = async () => {
    setLoading(true);
    try {
      const itemIds = aiData.selectedItems.map(i => i.id);
      await outfitsApi.create({ userId, name: aiData.name, itemIds, isAiGenerated: true, aiGenerationId: aiData.generationId });
      recordAiFeedback();
      setAiModal(false);
      setView('outfits');
      fetchOutfits();
    } catch (err) {
      handleApiAlert(err, 'Save failed');
    }
    finally { setLoading(false); }
  };

  // Chosen items = accepted; the shown-but-not-chosen alternatives = rejected (training labels).
  const recordAiFeedback = () => {
    if (!aiData?.generationId) return;
    const acceptedIds = new Set(aiData.selectedItems.map(i => i.id));
    const items = [];
    acceptedIds.forEach(id => items.push({ clothingItemId: id, action: 'Accepted' }));
    (aiData.recommendationsPerType || [])
      .flatMap(r => r.topCandidates || [])
      .forEach(c => { if (!acceptedIds.has(c.id)) items.push({ clothingItemId: c.id, action: 'Rejected' }); });
    if (items.length > 0) outfitsApi.recordFeedback(aiData.generationId, items).catch(() => {});
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
        color: toCsv(editItemData.color),
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

  const onWearOutfit = async (outfitOrId) => {
    const outfit = typeof outfitOrId === 'object'
      ? outfitOrId
      : outfits.find((candidate) => candidate.id === outfitOrId);
    const outfitId = outfit?.id || outfitOrId;

    try {
      await outfitsApi.recordWear(outfitId, { userId });
      pushToast({
        type: 'success',
        title: 'Wear recorded',
        message: `${outfit?.name || 'Outfit'} was added to today’s wear history.`,
      });
      refresh();
    } catch (err) {
      const message = getErrorMessage(err, 'Failed to record wear event.');
      console.error('Wear event error:', message);
      pushToast({ type: 'error', title: 'Wear not recorded', message });
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
      setCreateEventData({ name: '', type: 'Vacation', location: '', startDate: '', endDate: '', preferredStyles: [], reuseAfterDays: defaultReuseAfterDays });
      setCreateEventModal(false);
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
       const updatedEvent = events?.find(e => e.id === editEventData.id);
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
       const updatedEvent = events?.find(e => e.id === outfitEditingData.plannerEventId);
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


  return (
    <div className="sw-app">
      <input
        type="file"
        multiple
        ref={fileInputRef}
        onChange={handleFileChange}
        style={{ display: 'none' }}
        accept="image/*"
      />

      {/* ===== SIDEBAR ===== */}
      <aside className="sw-side">
        <div className="sw-brand">
          <div className="mark">W</div>
          <div>
            <div className="name">WardrobeManager</div>
          </div>
        </div>

        <nav className="sw-nav">
          <button className={`sw-nav-item is-cta${view === 'generate' ? ' is-active' : ''}`} onClick={() => setView('generate')}>
            <span className="ic">{IC.sparkles}</span>
            <span>Generate</span>
          </button>
          <button className={`sw-nav-item${view === 'wardrobe' ? ' is-active' : ''}`} onClick={() => setView('wardrobe')}>
            <span className="ic">{IC.hanger}</span>
            <span>Wardrobe</span>
            <span className="badge">{clothes.length}</span>
          </button>
          <button className={`sw-nav-item${view === 'outfits' ? ' is-active' : ''}`} onClick={() => setView('outfits')}>
            <span className="ic">{IC.layers}</span>
            <span>Outfits</span>
            <span className="badge">{outfits.length}</span>
          </button>
          <button className={`sw-nav-item${view === 'planner' ? ' is-active' : ''}`} onClick={() => setView('planner')}>
            <span className="ic">{IC.calendar}</span>
            <span>Planner</span>
            <span className="badge">{plannerEvents.length}</span>
          </button>
          <div className="sw-nav-grp">Insights</div>
          <button className={`sw-nav-item${view === 'stats' ? ' is-active' : ''}`} onClick={() => setView('stats')}>
            <span className="ic">{IC.chart}</span>
            <span>Stats</span>
            <span className="badge">{Math.round(usageRate)}%</span>
          </button>
          <div className="sw-nav-grp">Account</div>
          <button className={`sw-nav-item${view === 'settings' ? ' is-active' : ''}`} onClick={() => setView('settings')}>
            <span className="ic">{IC.settings}</span>
            <span>Settings</span>
          </button>
        </nav>

        <div className="sw-side-foot">
          <div className="sw-avatar">{userInitials}</div>
          <div className="who">
            {userDisplayName}
            <small>{userEmail}</small>
          </div>
          <button className="cog" onClick={toggleTheme} title={isDarkMode ? 'Switch to light' : 'Switch to dark'}>
            {isDarkMode ? IC.sun : IC.moon}
          </button>
        </div>
      </aside>

      {/* ===== MAIN ===== */}
      <main className="sw-main">
        <div className="sw-top">
          <div className="sw-mobile-brand">
            <div className="mark">W</div>
            WardrobeManager
          </div>
          <div className="ttl">
            {view === 'generate' ? 'Generate' : view === 'wardrobe' ? 'Wardrobe' : view === 'outfits' ? 'Outfits' : view === 'planner' ? 'Planner' : view === 'settings' ? 'Settings' : 'Stats'}
            <small>
              {view === 'generate' ? 'AI STYLIST' : view === 'wardrobe' ? `${clothes.length} ITEMS` : view === 'outfits' ? `${outfits.length} SAVED` : view === 'planner' ? `${plannerEvents.length} EVENTS` : view === 'settings' ? 'ACCOUNT' : 'INSIGHTS'}
            </small>
          </div>
          <div className="spacer" />
          <NotificationBell onActivate={(n) => {
            if (n.type === 'WeatherAlert') setView('planner');
          }} />
          <button className="sw-icon-btn" onClick={() => fileInputRef.current?.click()} title="Add clothing item">{IC.plus}</button>
          <button className="sw-icon-btn" onClick={onLogout} title="Log out">{IC.logout}</button>
        </div>

        <div className="sw-content">
          {view === 'generate' ? (
            <div className="sw-ed-page">
              <div className="sw-dash-top">
                <section className="sw-ootd-panel">
              <div className="sw-ootd-bar">
                <span className="sw-ed-eyebrow" style={{ margin: 0 }}>{todayLabel}</span>
                <div className="grow" />
                <div className="sw-ootd-actions">
                  <button className="sw-ed-ghost" onClick={onRediscover} disabled={loading || clothes.length === 0} title="Build an outfit around something you rarely wear">Rediscover</button>
                  <button className="sw-ed-ghost" onClick={() => loadOutfitOfDay(true)} disabled={ootdLoading} title="Pick a fresh look for today">{ootdLoading ? 'Styling…' : 'Shuffle ↻'}</button>
                  <button className="sw-ed-go" onClick={() => onGenerate()} disabled={loading || clothes.length === 0}>
                    {loading ? 'Generating...' : useGemmaStylistForOutfits ? 'Style with Gemma3' : 'Generate'}
                  </button>
                </div>
              </div>

              <div className="sw-ootd">
                {ootd ? (
                  <div className="sw-ootd-strip" onClick={openOutfitOfDay} role="button" tabIndex={0}>
                    <div className="sw-ootd-thumbs">
                      {(ootd.selectedItems || []).map(it => (
                        <div key={it.id} className="t"><img src={it.processedImageUrl} alt={it.name} /></div>
                      ))}
                    </div>
                    <div className="sw-ootd-mid">
                      <div className="h">{ootdInsight?.headline || ootd.name || 'Today’s look'}</div>
                      <div className="a">
                        {ootdInsightLoading && !ootdInsight
                          ? 'composing today’s idea…'
                          : (ootdInsight?.weatherAdvice || 'A fresh look featuring a piece you rarely wear.')}
                      </div>
                    </div>
                    <div className="sw-ootd-rt">
                      <button
                        className="wx"
                        onClick={(e) => { e.stopPropagation(); setSearchTerm(''); setCityModal(true); }}
                        title="Change city"
                      >
                        <strong>{weatherInfo ? `${Math.round(weatherInfo.temperature)}°` : '--°'}</strong>
                        <span>{weatherInfo?.conditionDetail || weatherInfo?.condition || 'updating…'}</span>
                        <span className="loc">{weatherLocationLabel}</span>
                        {weatherInfo?.rainChance != null && weatherInfo.rainChance > 0 && (
                          <span className="r">{weatherInfo.rainChance}%</span>
                        )}
                      </button>
                      <span className="cta">View &amp; save →</span>
                    </div>
                  </div>
                ) : (
                  <div className="sw-ootd-empty">{ootdLoading ? 'Putting together today’s look…' : 'No look yet — hit Shuffle to style one.'}</div>
                )}
              </div>

                </section>

                <aside className="sw-dash-rail">
                  <button
                    className="sw-dash-weather"
                    onClick={() => { setSearchTerm(''); setCityModal(true); }}
                    title="Change city"
                  >
                    <span className="sw-dash-weather-title" title={`Weather in ${weatherLocationLabel}`}>
                      Weather <b>{weatherLocationLabel}</b>
                    </span>
                    <strong>{weatherInfo ? `${Math.round(weatherInfo.temperature)}\u00B0` : '--\u00B0'}</strong>
                    <small>{weatherInfo?.conditionDetail || weatherInfo?.condition || 'updating...'}</small>
                    {weatherInfo?.rainChance != null && weatherInfo.rainChance > 0 && (
                      <em>{weatherInfo.rainChance}% rain</em>
                    )}
                  </button>

                  <div className="sw-dash-stats">
                    <div><span>Wardrobe</span><strong>{clothes.length}</strong></div>
                    <div><span>Looks</span><strong>{outfits.length}</strong></div>
                    <div><span>Usage</span><strong>{Math.round(usageRate)}%</strong></div>
                  </div>

                  {nextUpEvent && (
                    <div className="sw-dash-next">
                      <span>Next up</span>
                      <h3>{nextUpEvent.event.name}</h3>
                      <p>
                        {nextUpEvent.daysUntil === 0 ? 'Today' : nextUpEvent.daysUntil === 1 ? 'Tomorrow' : `In ${nextUpEvent.daysUntil} days`}
                        {' · '}
                        {nextUpEvent.event.location}
                      </p>
                      <button
                        className="sw-ed-ghost"
                        onClick={() => { setSelectedPlannerEvent(nextUpEvent.event); setSelectedDayIndex(null); setView('planner'); }}
                      >
                        {nextUpEvent.needsPlan ? 'Plan outfit' : 'View planner'}
                      </button>
                    </div>
                  )}
                </aside>
              </div>

              {outfits.length > 0 && (
                <div className="sw-ed-sec">
                  <div className="sw-ed-sec-h">
                    <span className="sw-ed-eyebrow" style={{ margin: 0 }}>Recent looks</span>
                    <div className="grow" />
                    <button className="sw-ed-more" onClick={() => setView('outfits')}>See all →</button>
                  </div>
                  <div className="sw-recent">
                    {outfits.slice(0, 6).map(o => (
                      <div key={o.id} className="sw-recent-card">
                        <div className="sw-recent-thumbs" onClick={() => setSelectedItem(o.items?.[0] || null)} role="button" tabIndex={0}>
                          {o.items?.slice(0, 4).map(item => (
                            <div key={item.id} className="o-thumb">
                              <img src={item.processedImageUrl} alt={item.name} />
                            </div>
                          ))}
                        </div>
                        <div className="sw-recent-info">
                          <div className="ttl" title={o.name}>{o.name}</div>
                          <div className="sub">{new Date(o.createdAt).toLocaleDateString()}{o.isAiGenerated ? ' · AI' : ''}</div>
                        </div>
                        <button className="sw-recent-wear" onClick={() => onWearOutfit(o)}>Wear</button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {forgottenItems.length > 0 && (
                <div className="sw-ed-sec">
                  <div className="sw-ed-sec-h">
                    <span className="sw-ed-eyebrow" style={{ margin: 0 }}>Rediscover</span>
                    <div className="grow" />
                    <button className="sw-ed-more" onClick={() => setView('stats')}>See usage →</button>
                  </div>
                  <div className="sw-forgotten">
                    {forgottenItems.slice(0, 6).map(it => (
                      <div key={it.id} className="sw-forgotten-card">
                        <div className="sw-forgotten-img">
                          <img src={it.imageUrl} alt={it.name} />
                          {it.daysSinceLastWear != null && <span className="sw-forgotten-days">{it.daysSinceLastWear}d</span>}
                        </div>
                        <div className="sw-forgotten-name" title={it.name}>{it.name}</div>
                        <button className="sw-forgotten-cta" onClick={() => buildAroundForgotten(it)} disabled={loading}>
                          Build a look →
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="sw-ed-sec">
                <div className="sw-ed-sec-h">
                  <span className="sw-ed-eyebrow" style={{ margin: 0 }}>This week</span>
                  <div className="grow" />
                  <button className="sw-ed-more" onClick={openPlannerForToday}>Open planner →</button>
                </div>
                <div className="sw-ed-week">
                  {upcomingWeekDays.map((day) => (
                    <div
                      key={day.dayKey}
                      className={`sw-ed-day ${day.isToday ? 'today' : ''} ${day.status}`}
                      onClick={() => setPreviewDay(day)}
                    >
                      {day.status !== 'free' && <span className="flag" />}
                      <div className="wd">{day.weekdayLabel}</div>
                      <div className="dn">{day.date.getDate()}</div>
                      <div className="wx">
                        {day.weather?.temperature !== undefined
                          ? `${Math.round(day.weather.temperature)}° ${day.weather.condition}`
                          : '—'}
                      </div>
                      {day.primaryEvent ? (
                        <div className="ev">
                          {day.primaryEvent.name}{day.totalEvents > 1 ? ` +${day.totalEvents - 1}` : ''}
                          <small>{day.status === 'planned' ? 'Planned' : 'Needs outfit'}</small>
                        </div>
                      ) : (
                        <div className="free">— free</div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : view === 'wardrobe' ? (
            <div className="sw-ed-page">
              <div className="sw-filter-bar">
                <div className="sw-search">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
                  <input placeholder="Search your wardrobe…" value={wardrobeSearch} onChange={e => setWardrobeSearch(e.target.value)} />
                </div>
                <div className="sw-seg" role="tablist">
                  {['ALL', ...CLOTHING_TYPES].map(t => (
                    <button key={t} className={wardrobeTypeFilter === t ? 'on' : ''} onClick={() => setWardrobeTypeFilter(t)}>
                      {t === 'ALL' ? 'All' : t}
                    </button>
                  ))}
                </div>
                <span className="sw-label-mono">{filteredClothes.length} items</span>
              </div>
              {wardrobeTags.length > 0 && (
                <div className="sw-filters">
                  <button
                    className={`sw-pill${wardrobeTagFilter === null ? ' is-active' : ''}`}
                    onClick={() => setWardrobeTagFilter(null)}
                  >
                    All tags
                  </button>
                  {wardrobeTags.map(t => (
                    <button
                      key={t}
                      className={`sw-pill${wardrobeTagFilter === t ? ' is-active' : ''}`}
                      onClick={() => setWardrobeTagFilter(wardrobeTagFilter === t ? null : t)}
                    >
                      + {t}
                    </button>
                  ))}
                </div>
              )}
              <div className="sw-wrd-grid">
                <button className="sw-add-tile" onClick={() => fileInputRef.current?.click()}>
                  <div className="sw-add-tile-icon">{IC.plus}</div>
                  <span>Add item</span>
                </button>
                {filteredClothes.map(item => (
                  <div key={item.id} className="sw-item" onClick={() => setSelectedItem(item)}>
                    <div className="thumb">
                      <img src={item.processedImageUrl} alt={item.name} />
                      <button className="del-btn" onClick={e => { e.stopPropagation(); onDelete('cloth', item.id); }}>remove</button>
                    </div>
                    <div className="meta">
                      <span className="name">{item.name}</span>
                      <span className="sub">{typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type}</span>
                    </div>
                  </div>
                ))}
                {filteredClothes.length === 0 && (
                  <div style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '60px 20px' }}>
                    <p style={{ color: 'var(--fg-muted)', fontSize: '0.9rem' }}>
                      {clothes.length === 0 ? 'Your wardrobe is empty. Add your first item!' : 'No items match those filters.'}
                    </p>
                  </div>
                )}
              </div>
            </div>
          ) : view === 'outfits' ? (
            <div className="sw-stack">
              <div className="sw-section-h">
                <h2>Saved outfits</h2>
                <span className="meta">{outfits.filter(o => outfitFilter === 'all' || o.isFavorite).length} looks</span>
                <div className="grow" />
                <div className="sw-seg">
                  <button className={outfitFilter === 'all' ? 'on' : ''} onClick={() => setOutfitFilter('all')}>All</button>
                  <button className={outfitFilter === 'favorites' ? 'on' : ''} onClick={() => setOutfitFilter('favorites')}>Favorites</button>
                </div>
                <div className="sw-seg">
                  <button className={outfitView === 'grid' ? 'on' : ''} onClick={() => setOutfitView('grid')}>Grid</button>
                  <button className={outfitView === 'list' ? 'on' : ''} onClick={() => setOutfitView('list')}>List</button>
                </div>
                <button className="sw-btn" onClick={() => setCustomOutfitModal(true)}>{IC.plus}<span>Create outfit</span></button>
              </div>

              {outfitView === 'grid' ? (
                <div className="sw-results sw-rise">
                  {outfits.filter(o => outfitFilter === 'all' || o.isFavorite).map(o => (
                    <div key={o.id} className="sw-outfit-card">
                      <div className="hd">
                        <div>
                          <div className="ttl">{o.name}</div>
                          <div className="sub">{new Date(o.createdAt).toLocaleDateString()}{o.isAiGenerated ? ' · AI' : ''}</div>
                        </div>
                        <div className="grow" />
                        <button
                          className={`heart-btn${o.isFavorite ? ' on' : ''}`}
                          onClick={() => onToggleFavorite(o)}
                          title={o.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
                        >
                          <svg width="16" height="16" viewBox="0 0 24 24" fill={o.isFavorite ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1.1L12 21l7.8-7.5 1-1.1a5.5 5.5 0 0 0 0-7.8Z"/></svg>
                        </button>
                      </div>
                      <div className="sw-outfit-grid lg">
                        {o.items?.slice(0, 5).map((item) => (
                          <div key={item.id} className="o-cell" onClick={() => setSelectedItem(item)}>
                            <img src={item.processedImageUrl} alt={item.name} />
                            <span className="tag">{typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type}</span>
                          </div>
                        ))}
                      </div>
                      <div className="ft">
                        <div className="tags">
                          {o.tags?.length > 0
                            ? o.tags.map(t => <span key={t} className="sw-tag">{t}</span>)
                            : <span className="sw-tag">{o.isAiGenerated ? 'AI' : 'Custom'}</span>
                          }
                        </div>
                        <div className="grow" />
                        <button className="sw-del-btn" onClick={() => onDelete('outfit', o.id)} title="Delete">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>
                        <button className="sw-btn ghost" onClick={() => { setEditData({ id: o.id, name: o.name, itemIds: o.items?.map(i => i.id) || [], tags: o.tags || [] }); setEditModal(true); }}>Edit</button>
                        <button className="sw-btn" onClick={() => onWearOutfit(o)}>Wear</button>
                      </div>
                    </div>
                  ))}
                  {outfits.filter(o => outfitFilter === 'all' || o.isFavorite).length === 0 && (
                    <div className="sw-empty">
                      <h3>{outfitFilter === 'favorites' ? 'No favorites yet' : 'No outfits yet'}</h3>
                      <p>{outfitFilter === 'favorites' ? 'Heart an outfit to add it here.' : 'Generate your first AI outfit or create one manually.'}</p>
                      {outfitFilter === 'all' && (
                        <button className="sw-btn accent" onClick={() => onGenerate()} disabled={clothes.length === 0}>Generate outfit</button>
                      )}
                    </div>
                  )}
                </div>
              ) : (
                <div className="sw-outfit-list sw-rise">
                  {outfits.filter(o => outfitFilter === 'all' || o.isFavorite).map(o => (
                    <div key={o.id} className="sw-outfit-list-item">
                      <div className="sw-outfit-list-thumbs">
                        {o.items?.slice(0, 4).map(item => (
                          <div key={item.id} className="o-thumb">
                            <img src={item.processedImageUrl} alt={item.name} />
                          </div>
                        ))}
                      </div>
                      <div className="sw-outfit-list-info">
                        <div className="ttl">{o.name}</div>
                        <div className="sub">{new Date(o.createdAt).toLocaleDateString()}{o.isAiGenerated ? ' · AI' : ''}</div>
                        <div className="sw-outfit-list-tags">
                          {o.tags?.length > 0
                            ? o.tags.map(t => <span key={t} className="sw-tag">{t}</span>)
                            : <span className="sw-tag">{o.isAiGenerated ? 'AI' : 'Custom'}</span>
                          }
                        </div>
                      </div>
                      <div className="sw-outfit-list-actions">
                        <button
                          className={`heart-btn${o.isFavorite ? ' on' : ''}`}
                          onClick={() => onToggleFavorite(o)}
                          title={o.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
                        >
                          <svg width="16" height="16" viewBox="0 0 24 24" fill={o.isFavorite ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1.1L12 21l7.8-7.5 1-1.1a5.5 5.5 0 0 0 0-7.8Z"/></svg>
                        </button>
                        <button className="sw-del-btn" onClick={() => onDelete('outfit', o.id)} title="Delete">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>
                        <button className="sw-btn ghost" onClick={() => { setEditData({ id: o.id, name: o.name, itemIds: o.items?.map(i => i.id) || [], tags: o.tags || [] }); setEditModal(true); }}>Edit</button>
                        <button className="sw-btn" onClick={() => onWearOutfit(o)}>Wear</button>
                      </div>
                    </div>
                  ))}
                  {outfits.filter(o => outfitFilter === 'all' || o.isFavorite).length === 0 && (
                    <div className="sw-empty" style={{ gridColumn: 'unset' }}>
                      <h3>{outfitFilter === 'favorites' ? 'No favorites yet' : 'No outfits yet'}</h3>
                      <p>{outfitFilter === 'favorites' ? 'Heart an outfit to add it here.' : 'Generate your first AI outfit or create one manually.'}</p>
                      {outfitFilter === 'all' && (
                        <button className="sw-btn accent" onClick={() => onGenerate()} disabled={clothes.length === 0}>Generate outfit</button>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          ) : view === 'planner' ? (
            <div className="sw-planner-layout">

              {/* ── Left rail: event list ── */}
              <div className="sw-planner-rail">
                <div className="sw-planner-rail-top">
                  <div className="sw-section-h" style={{ marginBottom: 12 }}>
                    <h2>Events</h2>
                    <div style={{ flex: 1 }} />
                    <button className="sw-btn" onClick={() => {
                      setCreateEventData((data) => ({ ...data, reuseAfterDays: defaultReuseAfterDays }));
                      setCreateEventModal(true);
                    }}>
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                      <span>New</span>
                    </button>
                  </div>
                  <div className="sw-seg" style={{ width: '100%' }}>
                    <button className={plannerEventTab === 'active' ? 'on' : ''} onClick={() => setPlannerEventTab('active')}>
                      Active {plannerEvents.length > 0 && <span className="sw-seg-count">{plannerEvents.length}</span>}
                    </button>
                    <button className={plannerEventTab === 'archived' ? 'on' : ''} onClick={() => setPlannerEventTab('archived')}>
                      Archived {archivedPlannerEvents.length > 0 && <span className="sw-seg-count">{archivedPlannerEvents.length}</span>}
                    </button>
                  </div>
                </div>

                <div className="sw-planner-event-list">
                  {(plannerEventTab === 'active' ? plannerEvents : archivedPlannerEvents).length === 0 ? (
                    <div className="sw-planner-rail-empty">
                      {plannerEventTab === 'active' ? 'No events yet' : 'No archived events'}
                    </div>
                  ) : (
                    (plannerEventTab === 'active' ? plannerEvents : archivedPlannerEvents).map(event => {
                      const isActive = selectedPlannerEvent?.id === event.id;
                      const totalDays = Math.ceil((new Date(event.endDate) - new Date(event.startDate)) / 86400000) + 1;
                      const plannedDays = event.itineraries?.length || 0;
                      return (
                        <div
                          key={event.id}
                          className={`sw-planner-event-card${isActive ? ' active' : ''}`}
                          onClick={() => { setSelectedPlannerEvent(event); setSelectedDayIndex(null); fetchEventForecast(event); }}
                        >
                          <div className="spec-icon">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2"/><line x1="12" y1="12" x2="12" y2="16"/><line x1="10" y1="14" x2="14" y2="14"/></svg>
                          </div>
                          <div className="spec-body">
                            <div className="spec-name">{event.name}</div>
                            <div className="spec-meta">
                              {event.location && <span>{event.location}</span>}
                              <span>{new Date(event.startDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – {new Date(event.endDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</span>
                            </div>
                            <div className="spec-progress">
                              <div className="spec-prog-bar">
                                <span style={{ width: totalDays > 0 ? `${(plannedDays / totalDays) * 100}%` : '0%' }} />
                              </div>
                              <span className="spec-prog-label">{plannedDays}/{totalDays} days planned</span>
                            </div>
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>

              {/* ── Main content ── */}
              <div className="sw-planner-main">
                {!selectedPlannerEvent ? (
                  <div className="sw-empty" style={{ height: '100%', gridColumn: 'unset' }}>
                    <h3>No event selected</h3>
                    <p>Pick an event from the list, or create a new one.</p>
                    <button className="sw-btn accent" onClick={() => {
                      setCreateEventData((data) => ({ ...data, reuseAfterDays: defaultReuseAfterDays }));
                      setCreateEventModal(true);
                    }}>New event</button>
                  </div>

                ) : selectedDayIndex !== null && plannerDays[selectedDayIndex] ? (() => {
                  const day = plannerDays[selectedDayIndex];
                  const itin = selectedDayItinerary;
                  const outfitItems = itin?.outfit?.items || [];
                  return (
                    <div className="sw-stack">
                      {/* Back + day label */}
                      <div className="sw-planner-day-nav">
                        <button className="sw-btn ghost sw-planner-back" onClick={() => setSelectedDayIndex(null)}>
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="15 18 9 12 15 6"/></svg>
                          <span>{selectedPlannerEvent.name}</span>
                        </button>
                        <div style={{ flex: 1 }} />
                        <span className="sw-label-mono">{selectedPlannerEvent.location}</span>
                      </div>

                      {/* Day hero */}
                      <div className="sw-today-hero">
                        <div className="left">
                          <div className="day-head">
                            <div>
                              <div className="lbl">DAY {day.dayNumber} · {day.date.toLocaleDateString(undefined, { weekday: 'long' }).toUpperCase()} {day.date.getDate()} {day.date.toLocaleDateString(undefined, { month: 'long' }).toUpperCase()}</div>
                              <h2>{itin?.outfit?.name || <em>No outfit yet</em>}</h2>
                            </div>
                          </div>

                          {day.weather && (
                            <div className="weather-strip">
                              <span className="ic">
                                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
                                  {/rain/i.test(day.weather.condition) ? <><line x1="16" y1="13" x2="16" y2="21"/><line x1="8" y1="13" x2="8" y2="21"/><line x1="12" y1="15" x2="12" y2="23"/><path d="M20 16.58A5 5 0 0 0 18 7h-1.26A8 8 0 1 0 4 15.25"/></>
                                  : /cloud/i.test(day.weather.condition) ? <><path d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z"/></>
                                  : <><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></>}
                                </svg>
                              </span>
                              <div className="txt">
                                <div className="a">{Math.round(day.weather.temperature)}°C</div>
                                <div className="b">{day.weather.condition}</div>
                              </div>
                              {itin && (
                                <div className="match">
                                  <div className="a" style={{ fontSize: 13 }}>{itin.moment}</div>
                                  <div className="b">OCCASION</div>
                                </div>
                              )}
                            </div>
                          )}

                          <div className="actions">
                            {itin ? (
                              <>
                                <button className="sw-btn" onClick={() => onRegenerateItinerary(selectedPlannerEvent.id, itin.id)} disabled={loading}>
                                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg>
                                  <span>Regenerate</span>
                                </button>
                                <button className="sw-btn ghost" onClick={() => openOutfitEditingModal(selectedPlannerEvent.id, itin, day, selectedDayIndex)}>Edit outfit</button>
                                <button className="sw-btn ghost" onClick={() => openEditItineraryModal(selectedPlannerEvent.id, itin)}>Edit details</button>
                                <button className="sw-del-btn" onClick={() => { onDeleteItinerary(selectedPlannerEvent.id, itin.id); setSelectedDayIndex(null); }} title="Remove outfit">
                                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                                </button>
                              </>
                            ) : (
                              <button className="sw-btn accent" onClick={() => openOutfitEditingModal(selectedPlannerEvent.id, null, day, selectedDayIndex, 'plan')}>
                                Plan outfit for this day
                              </button>
                            )}
                          </div>
                        </div>

                        <div className="right">
                          {outfitItems.length > 0 ? (
                            <>
                              <div className="outfit-pre-name">
                                <span className="nm">{itin.outfit?.name}</span>
                                <span className="lb">{outfitItems.length} items</span>
                              </div>
                              <div className="outfit-pre">
                                {outfitItems.slice(0, 5).map(item => (
                                  <div key={item.id} className="o-cell" onClick={() => setSelectedItem(item)}>
                                    <img src={item.processedImageUrl} alt={item.name} />
                                    <span className="tag">{typeof item.type === 'number' ? CLOTHING_TYPES[item.type] : item.type}</span>
                                  </div>
                                ))}
                              </div>
                            </>
                          ) : (
                            <div className="sw-planner-day-empty-right">
                              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" style={{ color: 'var(--fg-muted)', opacity: .4 }}><rect x="3" y="3" width="18" height="18" rx="3"/><line x1="9" y1="3" x2="9" y2="21"/></svg>
                              <span>No outfit assigned yet</span>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })() : (
                  /* Event overview: trip header + week strip */
                  <div className="sw-stack">
                    {/* Trip header */}
                    <div className="sw-trip">
                      <div className="hd">
                        <div className="where">
                          <div style={{ width: 52, height: 52, borderRadius: 13, background: 'var(--bg-soft)', border: '1px solid var(--border)', display: 'grid', placeItems: 'center', color: 'var(--fg-muted)', flexShrink: 0 }}>
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2"/><line x1="12" y1="12" x2="12" y2="16"/><line x1="10" y1="14" x2="14" y2="14"/></svg>
                          </div>
                          <div>
                            <h3>{selectedPlannerEvent.name}</h3>
                            <div className="when">
                              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ display: 'inline', marginRight: 4 }}><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
                              {selectedPlannerEvent.location} · {new Date(selectedPlannerEvent.startDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – {new Date(selectedPlannerEvent.endDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} · {plannerDays.length} days
                            </div>
                          </div>
                        </div>
                        <div style={{ flex: 1 }} />
                        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
                          <button className="sw-btn accent" onClick={() => handlePackSmart(selectedPlannerEvent.id)} disabled={loading}>
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2"/></svg>
                            <span>Pack Smart</span>
                          </button>
                          <button className="sw-btn" onClick={() => onGenerateEventOutfits(selectedPlannerEvent.id)} disabled={loading}>
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/></svg>
                            <span>Generate outfits</span>
                          </button>
                          <button className="sw-btn ghost" onClick={() => { setEditEventData({ id: selectedPlannerEvent.id, name: selectedPlannerEvent.name, type: selectedPlannerEvent.type, location: selectedPlannerEvent.location, startDate: selectedPlannerEvent.startDate.split('T')[0], endDate: selectedPlannerEvent.endDate.split('T')[0], preferredStyles: selectedPlannerEvent.preferredStyles || [], reuseAfterDays: selectedPlannerEvent.reuseAfterDays || null }); setEditEventModal(true); }}>Edit</button>
                          <button className="sw-del-btn" title="Archive" onClick={() => { if(confirm('Archive this event?')) onArchiveEvent(selectedPlannerEvent.id); }} disabled={loading}>
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="21 8 21 21 3 21 3 8"/><rect x="1" y="3" width="22" height="5"/><line x1="10" y1="12" x2="14" y2="12"/></svg>
                          </button>
                          <button className="sw-del-btn" title="Delete" onClick={() => { if(confirm('Delete this event?')) { onDeletePlannerEvent(selectedPlannerEvent.id); setSelectedPlannerEvent(null); } }}>
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                          </button>
                        </div>
                      </div>

                      {/* Packing progress */}
                      {(() => {
                        const total = plannerDays.length;
                        const planned = plannerDays.filter(d => d.itinerary).length;
                        return (
                          <div className="sw-progress">
                            <span className="label">Outfits planned</span>
                            <div className="bar"><span style={{ width: total > 0 ? `${(planned / total) * 100}%` : '0%' }} /></div>
                            <span className="val">{planned} / {total}</span>
                          </div>
                        );
                      })()}

                      {/* Day strip */}
                      <div className="trip-strip">
                        {plannerDays.map((day, idx) => {
                          const items = day.itinerary?.outfit?.items?.slice(0, 4) || [];
                          return (
                            <div key={idx} className="td" onClick={() => setSelectedDayIndex(idx)}>
                              <div className="h">
                                <span className="wd">{day.date.toLocaleDateString(undefined, { weekday: 'short' }).toUpperCase()}</span>
                                <span className="dn">{day.date.getDate()}</span>
                                <span style={{ flex: 1 }} />
                                {day.weather && (
                                  <span className="cond">
                                    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                      {/rain/i.test(day.weather.condition) ? <><path d="M20 16.58A5 5 0 0 0 18 7h-1.26A8 8 0 1 0 4 15.25"/><line x1="8" y1="19" x2="8" y2="21"/><line x1="8" y1="13" x2="8" y2="15"/><line x1="16" y1="19" x2="16" y2="21"/><line x1="16" y1="13" x2="16" y2="15"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="12" y1="15" x2="12" y2="17"/></>
                                      : /cloud/i.test(day.weather.condition) ? <path d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z"/>
                                      : <><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/></>}
                                    </svg>
                                    {Math.round(day.weather.temperature)}°
                                  </span>
                                )}
                              </div>
                              <div className="mini-out">
                                {items.length > 0 ? items.map(item => (
                                  <div key={item.id} className="q">
                                    <img src={item.processedImageUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'contain', padding: 3 }} />
                                  </div>
                                )) : (
                                  <div style={{ gridColumn: '1 / -1', display: 'grid', placeItems: 'center', color: 'var(--fg-muted)', opacity: .5 }}>
                                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                                  </div>
                                )}
                              </div>
                              <div style={{ fontSize: 11.5, fontWeight: 500, lineHeight: 1.2 }}>{day.itinerary?.outfit?.name || 'No outfit'}</div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          ) : view === 'settings' ? (
            <SettingsSection
              userInitials={userInitials}
              userDisplayName={userDisplayName}
              userEmail={userEmail}
              memberSince={memberSince}
              city={city}
              onOpenCityModal={() => { setSearchTerm(''); setCityModal(true); }}
              isDarkMode={isDarkMode}
              toggleTheme={handleToggleTheme}
              onLogout={onLogout}
              onSaveProfile={handleSaveProfile}
              onSavePreferences={handleSavePreferences}
              onDeleteAccount={handleDeleteAccount}
              favoriteColors={user?.favoriteColors || user?.FavoriteColors || []}
              avoidColors={user?.avoidColors || user?.AvoidColors || []}
              outerwearMode={user?.outerwearMode ?? user?.OuterwearMode ?? 'auto'}
              outerwearTempThreshold={user?.outerwearTempThreshold ?? user?.OuterwearTempThreshold ?? 23}
              varietyLevel={user?.varietyLevel ?? user?.VarietyLevel ?? 'normal'}
              blockDuplicateUploads={user?.blockDuplicateUploads ?? user?.BlockDuplicateUploads ?? false}
              preferLightOnHotDays={user?.preferLightOnHotDays ?? user?.PreferLightOnHotDays ?? true}
              useGemmaStylistForOutfits={useGemmaStylistForOutfits}
              defaultReuseAfterDays={defaultReuseAfterDays}
              clothes={clothes}
              outfits={outfits}
              aiOutfitCount={aiOutfitCount}
            />
          ) : (
            <div className="stats-layout">
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
        subtypeOptions={subtypeOptions}
        onUpdateItem={onUpdateItem}
        onGenerate={onGenerate}
        loading={loading}
        onSelectSimilar={(item) => {
          // Swap the inspected item to a similar one; prefer the full wardrobe record if we have it.
          const full = clothes.find(c => c.id === item.id) || item;
          setEditItemMode(false);
          setSelectedItem(full);
        }}
      />

      <StyleSelectionModal
        isOpen={styleSelectionModal}
        onClose={() => setStyleSelectionModal(false)}
        executeGeneration={executeGeneration}
        isRediscover={generationContext === 'rediscover'}
        preferUnused={preferUnused}
        setPreferUnused={setPreferUnused}
        useGemmaStylistForOutfits={useGemmaStylistForOutfits}
      />

      <AiSuggestionModal
        isOpen={aiModal}
        onClose={() => { setAiModal(false); setAiStylingNotes([]); setAiInsight(null); }}
        aiData={aiData}
        setAiData={setAiData}
        stylingNotes={aiStylingNotes}
        notesLoading={notesLoading}
        insight={aiInsight}
        onSaveAiOutfit={onSaveAiOutfit}
        onRegenerate={null}
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
          setCreateEventData({ name: "", type: "Vacation", location: "", startDate: "", endDate: "", preferredStyles: [], reuseAfterDays: defaultReuseAfterDays });
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
        defaultReuseAfterDays={defaultReuseAfterDays}
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

      {/* ===== MOBILE BOTTOM NAV ===== */}
      <nav className="sw-bottom">
        <div className="sw-bottom-grid">
          <button className={`sw-tab${view === 'wardrobe' ? ' is-active' : ''}`} onClick={() => setView('wardrobe')}>
            <span className="ic">{IC.hanger}</span><span>Wardrobe</span>
          </button>
          <button className={`sw-tab${view === 'outfits' ? ' is-active' : ''}`} onClick={() => setView('outfits')}>
            <span className="ic">{IC.layers}</span><span>Outfits</span>
          </button>
          <button className="sw-tab-cta" onClick={() => setView('generate')} aria-label="Generate">
            {IC.sparkles}
          </button>
          <button className={`sw-tab${view === 'planner' ? ' is-active' : ''}`} onClick={() => setView('planner')}>
            <span className="ic">{IC.calendar}</span><span>Planner</span>
          </button>
          <button className={`sw-tab${view === 'stats' ? ' is-active' : ''}`} onClick={() => setView('stats')}>
            <span className="ic">{IC.chart}</span><span>Stats</span>
          </button>
        </div>
      </nav>
    </div>
  );
};

export default DashboardPage;

