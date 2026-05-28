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
import SettingsSection from '../components/SettingsSection';
import WeatherAlertNotice from '../components/WeatherAlertNotice';
import { authApi, clothingApi, geoApi, outfitsApi, plannerEventsApi, statsApi } from '../services/wardrobeApi';
import { COLORS, CLOTHING_TYPES, GENDERS, SEASONS, USAGES, EVENT_MOMENTS } from '../constants/wardrobe';
import { getErrorMessage } from '../utils/errors';
import { toCsv, toTypeIndex } from '../utils/wardrobeTransforms';
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

const PROMPT_TAGS = [
  { group: 'Occasion', items: [
    { label: 'Office',      text: 'office day, polished' },
    { label: 'Date night',  text: 'dinner date, elegant' },
    { label: 'Interview',   text: 'job interview, formal' },
    { label: 'Park walk',   text: 'walk in the park, casual, comfortable' },
    { label: 'Brunch',      text: 'brunch with friends, relaxed' },
    { label: 'Travel',      text: 'travel day, comfortable' },
    { label: 'Party',       text: 'party, festive look' },
    { label: 'Night out',   text: 'night out, club, bold' },
    { label: 'Hiking',      text: 'hiking, outdoor, sporty' },
    { label: 'Shopping',    text: 'shopping, casual' },
    { label: 'Concert',     text: 'concert, cool, statement' },
    { label: 'Sport',       text: 'gym workout, sporty' },
  ]},
  { group: 'Style', items: [
    { label: 'Minimal',     text: 'minimal, clean look' },
    { label: 'Bold',        text: 'bold, statement piece' },
    { label: 'Classic',     text: 'classic, timeless' },
    { label: 'Cozy',        text: 'cozy, comfortable' },
    { label: 'Chic',        text: 'chic, polished' },
    { label: 'Streetwear',  text: 'streetwear, urban' },
  ]},
  { group: 'Weather', items: [
    { label: 'Hot',         text: 'hot weather, light fabrics' },
    { label: 'Mild',        text: 'mild, light layers' },
    { label: 'Cold',        text: 'cold, warm layers' },
    { label: 'Rainy',       text: 'rainy day, practical' },
  ]},
];

const RECOGNIZERS = [
  { category: 'occasion', keywords: [
    'office', 'work', 'meeting', 'interview', 'date', 'dinner', 'party', 'gym', 'sport', 'workout',
    'travel', 'flight', 'airport', 'beach', 'wedding', 'brunch', 'errands', 'casual friday',
    'park', 'walk', 'stroll', 'plimbare', 'hiking', 'hike', 'nature', 'outdoor', 'outdoors',
    'shopping', 'picnic', 'concert', 'festival', 'club', 'night out', 'bar', 'birthday',
    'conference', 'networking', 'lunch', 'coffee', 'road trip', 'sightseeing', 'museum',
  ]},
  { category: 'style', keywords: [
    'formal', 'smart casual', 'casual', 'elegant', 'minimal', 'classic', 'trendy', 'bold',
    'cozy', 'relaxed', 'polished', 'chic', 'streetwear', 'sporty', 'edgy', 'preppy',
    'comfortable', 'professional', 'business', 'artsy', 'urban', 'boho', 'feminine', 'sharp',
  ]},
  { category: 'weather', keywords: [
    'warm', 'hot', 'cold', 'rain', 'rainy', 'sunny', 'winter', 'summer', 'spring', 'autumn',
    'snow', 'windy', 'layered', 'light fabric', 'heavy', 'freezing', 'mild', 'breezy', 'cloudy',
  ]},
  { category: 'type', keywords: [
    'dress', 'jeans', 'shirt', 'suit', 'jacket', 'coat', 'sneakers', 'heels', 'boots',
    'trousers', 'skirt', 'blouse', 'sweater', 't-shirt', 'pants', 'shorts', 'hoodie',
    'cardigan', 'blazer', 'loafers', 'sandals', 'scarf', 'hat', 'cap', 'vest',
  ]},
];

// Maps prompt keywords → backend style values (USAGES)
const STYLE_MAP = [
  { target: 'Formal',       keywords: [
    'formal', 'interview', 'business', 'professional', 'conference', 'ceremony',
    'wedding', 'black tie', 'suit and tie', 'gala', 'nunta', 'cununie', 'botez',
    'interviu', 'ceremonie', 'eveniment oficial', 'banchet',
  ]},
  { target: 'Smart Casual', keywords: [
    'smart casual', 'office', 'work', 'dinner', 'date', 'restaurant', 'polished',
    'elegant', 'smart', 'business casual', 'meeting', 'networking', 'lunch',
    'birou', 'serviciu', 'intalnire', 'întâlnire', 'cina', 'cină',
  ]},
  { target: 'Party',        keywords: [
    'party', 'club', 'festive', 'celebration', 'night out', 'cocktail', 'birthday',
    'disco', 'bar', 'concert', 'festival',
    'petrecere', 'aniversare', 'ziua de nastere', 'zi de naștere', 'iesire in club',
  ]},
  { target: 'Sports',       keywords: [
    'sport', 'gym', 'workout', 'fitness', 'running', 'hiking', 'hike', 'training',
    'athletic', 'outdoor', 'active', 'jogging', 'cycling', 'tennis', 'yoga', 'sporty',
    'sala', 'sală', 'alergat', 'antrenament', 'drumetie', 'drumeție', 'munte',
  ]},
  { target: 'Travel',       keywords: [
    'travel', 'flight', 'airport', 'trip', 'journey', 'vacation', 'holiday',
    'road trip', 'sightseeing', 'backpacking',
    'calatorie', 'călătorie', 'voiaj', 'excursie', 'avion', 'aeroport', 'vacanta', 'vacanță',
  ]},
  { target: 'Casual',       keywords: [
    'casual', 'relaxed', 'everyday', 'errands', 'weekend', 'park', 'coffee', 'brunch',
    'stroll', 'walk', 'shopping', 'comfy', 'comfortable', 'chill', 'friends', 'picnic', 'museum', 'nature',
    'plimbare', 'relaxat', 'zilnic', 'parc', 'cafea', 'prieteni', 'cumparaturi', 'cumpărături',
  ]},
];

const KNOWN_CITIES = [
  // Romania
  'bucharest', 'cluj', 'cluj-napoca', 'timisoara', 'iasi', 'constanta', 'brasov', 'sibiu',
  'craiova', 'galati', 'ploiesti', 'oradea', 'braila', 'pitesti', 'arad', 'targu mures',
  // Europe
  'london', 'paris', 'berlin', 'rome', 'madrid', 'amsterdam', 'vienna', 'prague', 'budapest',
  'warsaw', 'athens', 'lisbon', 'barcelona', 'milan', 'brussels', 'zurich', 'geneva',
  'stockholm', 'oslo', 'copenhagen', 'helsinki', 'dublin', 'edinburgh', 'istanbul',
  'porto', 'seville', 'florence', 'venice', 'munich', 'hamburg', 'cologne', 'lyon',
  // World
  'dubai', 'abu dhabi', 'new york', 'los angeles', 'chicago', 'toronto', 'montreal',
  'sydney', 'melbourne', 'singapore', 'tokyo', 'bangkok', 'seoul', 'beijing', 'shanghai',
  'mumbai', 'delhi', 'miami', 'san francisco', 'boston', 'seattle', 'cape town',
];

const parsePrompt = (prompt) => {
  const lower = prompt.toLowerCase();
  let style = null;
  for (const { target, keywords } of STYLE_MAP) {
    if (keywords.some(kw => lower.includes(kw))) { style = target; break; }
  }
  let detectedCity = null;
  for (const c of KNOWN_CITIES) {
    if (lower.includes(c)) {
      detectedCity = c.split(' ').map(w => w[0].toUpperCase() + w.slice(1)).join(' ');
      break;
    }
  }
  return { style, city: detectedCity };
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
  const [genericForecast, setGenericForecast] = useState([]);
  const [clothes, setClothes] = useState([]);
  const [outfits, setOutfits] = useState([]);
  const [outfitFilter, setOutfitFilter] = useState('all'); // 'all', 'favorites'
  const [outfitView, setOutfitView] = useState('grid'); // 'grid', 'list'
  const [wardrobeSearch, setWardrobeSearch] = useState('');
  const [wardrobeTypeFilter, setWardrobeTypeFilter] = useState('ALL');
  const [wardrobeTagFilter, setWardrobeTagFilter] = useState(null);
  const [generatePrompt, setGeneratePrompt] = useState('');
  const [plannerEvents, setPlannerEvents] = useState([]);
  const [usageRate, setUsageRate] = useState(0);
  const [loading, setLoading] = useState(false);
  const [view, setView] = useState('generate');
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
  const [editData, setEditData] = useState({ id: null, name: '', itemIds: [], tags: [] });

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

  const handleSaveProfile = async (payload) => {
    const res = await authApi.updateUser(userId, payload);
    onUserUpdate(res.data);
  };

  const aiOutfitCount = useMemo(() => outfits.filter((outfit) => outfit.isAiGenerated).length, [outfits]);
  const customOutfitCount = useMemo(() => Math.max(outfits.length - aiOutfitCount, 0), [outfits.length, aiOutfitCount]);
  const appendTag = (text) => {
    setGeneratePrompt(prev => {
      const trimmed = prev.trimEnd();
      if (!trimmed) return text;
      return trimmed.endsWith(',') ? `${trimmed} ${text}` : `${trimmed}, ${text}`;
    });
  };

  const detectedConcepts = useMemo(() => {
    if (!generatePrompt.trim()) return [];
    const lower = generatePrompt.toLowerCase();
    const found = [];
    const seen = new Set();
    RECOGNIZERS.forEach(({ category, keywords }) => {
      keywords.forEach(kw => {
        if (lower.includes(kw) && !seen.has(kw)) {
          seen.add(kw);
          found.push({ category, text: kw });
        }
      });
    });
    for (const c of KNOWN_CITIES) {
      if (lower.includes(c)) {
        found.push({ category: 'city', text: c.split(' ').map(w => w[0].toUpperCase() + w.slice(1)).join(' ') });
        break;
      }
    }
    return found;
  }, [generatePrompt]);

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


  const todaysReadinessPercent = useMemo(() => {
    if (todaysEventSummary.totalEvents === 0) return 100;
    return Math.round((todaysEventSummary.plannedCount / todaysEventSummary.totalEvents) * 100);
  }, [todaysEventSummary]);


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

  const onGenerate = async (item = null) => {
    if (item) setSelectedItem(item);
    setGenerationContext(item ? 'item' : 'today');
    if (!item && generatePrompt.trim()) {
      let style = 'Casual';
      let promptCity = null;
      try {
        const res = await outfitsApi.parsePrompt(generatePrompt);
        style = res.data.style || 'Casual';
        promptCity = res.data.city || null;
      } catch {
        const fallback = parsePrompt(generatePrompt);
        style = fallback.style || 'Casual';
        promptCity = fallback.city;
      }
      executeGeneration(style, promptCity);
    } else {
      setStyleSelectionModal(true);
    }
  };

  const executeGeneration = async (style, overrideCity = null) => {
    setStyleSelectionModal(false);
    setLoading(true);
    const effectiveCity = overrideCity || city;

    let startItem = selectedItem;
    if (generationContext === 'today') {
      const candidates = style
        ? clothes.filter(c => c.usage?.toLowerCase().includes(style.toLowerCase()))
        : clothes;
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
        city: effectiveCity,
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
            <div className="name">SmartWardrobe</div>
            <div className="sub">Closet · AI · Calendar</div>
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
            SmartWardrobe
          </div>
          <div className="ttl">
            {view === 'generate' ? 'Generate' : view === 'wardrobe' ? 'Wardrobe' : view === 'outfits' ? 'Outfits' : view === 'planner' ? 'Planner' : view === 'settings' ? 'Settings' : 'Stats'}
            <small>
              {view === 'generate' ? 'AI STYLIST' : view === 'wardrobe' ? `${clothes.length} ITEMS` : view === 'outfits' ? `${outfits.length} SAVED` : view === 'planner' ? `${plannerEvents.length} EVENTS` : view === 'settings' ? 'ACCOUNT' : 'INSIGHTS'}
            </small>
          </div>
          <div className="spacer" />
          <button className="sw-icon-btn" onClick={() => fileInputRef.current?.click()} title="Add clothing item">{IC.plus}</button>
          <button className="sw-icon-btn" onClick={onLogout} title="Log out">{IC.logout}</button>
        </div>

        <div className="sw-content">
          {view === 'generate' ? (
            <div className="sw-stack">
              {weatherAlert && (
                <WeatherAlertNotice
                  alert={weatherAlert}
                  locationLabel={
                    weatherAlert?.plannerEventId && plannerEvents.find(e => e.id === weatherAlert.plannerEventId)?.location
                  }
                  onGenerateAlternative={() => {
                    if (weatherAlert?.plannerEventId) {
                      const event = plannerEvents.find(e => e.id === weatherAlert.plannerEventId);
                      if (event && weatherAlert.eventDate) {
                        const alertDate = new Date(weatherAlert.eventDate).toDateString();
                        const itinerary = event.itineraries.find(i => new Date(i.date).toDateString() === alertDate);
                        if (itinerary) {
                          onRegenerateItinerary(event.id, itinerary.id);
                          setWeatherAlert(null);
                          return;
                        }
                      }
                      onGenerateEventOutfits(weatherAlert.plannerEventId);
                    }
                  }}
                  onDismiss={() => setWeatherAlert(null)}
                />
              )}

              <div className="sw-gen-hero">
                <div className="copy">
                  <div className="sw-label-mono" style={{ marginBottom: 14 }}>· AI STYLIST</div>
                  <h1>What should you<br /><em>wear today?</em></h1>
                  <p>Describe the day, occasion, or mood. SmartWardrobe pairs items from your closet using fit, colour, weather and your calendar.</p>
                  <div className="stat-row">
                    <div className="stat"><div className="n">{clothes.length}</div><div className="l">Items</div></div>
                    <div className="stat"><div className="n">{outfits.length}</div><div className="l">Saved looks</div></div>
                    <div className="stat"><div className="n">{todaysEventSummary.totalEvents}</div><div className="l">Today&apos;s events</div></div>
                  </div>
                </div>
                <div className="sw-weather-card">
                  <button className="wc-city" onClick={() => { setSearchTerm(''); setCityModal(true); }}>{city}</button>
                  <div className="wc-temp">{weatherInfo ? `${Math.round(weatherInfo.temperature)}` : '--'}<sup>°</sup></div>
                  <div className="wc-cond">{weatherInfo?.condition || 'updating...'}</div>
                  <div className="wc-meta">season: {weatherInfo?.seasonSuggestion || 'n/a'}</div>
                  <div className="hero-readiness-block" style={{ marginTop: 'auto' }}>
                    <span>today readiness</span>
                    <strong>{todaysReadinessPercent}%</strong>
                    <div className="hero-progress-track">
                      <div className="hero-progress-fill" style={{ width: `${todaysReadinessPercent}%` }} />
                    </div>
                  </div>
                </div>
              </div>

              <div className="sw-prompt">
                <textarea
                  placeholder="e.g. Office meeting in the morning, then dinner with friends — want to look polished but comfortable…"
                  value={generatePrompt}
                  onChange={e => setGeneratePrompt(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) { e.preventDefault(); onGenerate(); } }}
                />
                {detectedConcepts.length > 0 && (
                  <div className="sw-detected">
                    <span className="sw-detected-lbl">recognized</span>
                    {detectedConcepts.map((c, i) => (
                      <span key={i} className={`sw-detected-badge sw-detected-badge--${c.category}`}>{c.text}</span>
                    ))}
                  </div>
                )}
                <div className="sw-prompt-tags">
                  {PROMPT_TAGS.map(group => (
                    <div key={group.group} className="sw-ptag-group">
                      <span className="sw-ptag-lbl">{group.group}</span>
                      <div className="sw-ptag-chips">
                        {group.items.map(tag => (
                          <button
                            key={tag.label}
                            className={`sw-pill${generatePrompt.toLowerCase().includes(tag.text.split(',')[0].toLowerCase()) ? ' is-active' : ''}`}
                            onClick={() => appendTag(tag.text)}
                          >
                            {tag.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
                <div className="sw-prompt-foot">
                  <span className="sw-label-mono">click generate or press ⌘↵</span>
                  <div style={{ flex: 1 }} />
                  <button className="sw-btn ghost" onClick={() => onGenerate()} disabled={loading || clothes.length === 0}>Surprise me</button>
                  <button className="sw-btn accent" onClick={() => onGenerate()} disabled={loading || clothes.length === 0}>
                    {IC.sparkles}<span>{loading ? 'Generating…' : 'Generate outfits'}</span>
                  </button>
                </div>
              </div>

              <div className="sw-week-section">
                <div className="sw-section-h">
                  <h2>Upcoming 7 days</h2>
                  <span className="meta">tap any day to jump into planner</span>
                  <div className="grow" />
                  <button className="sw-btn ghost" onClick={openPlannerForToday}>Open planner</button>
                </div>
                <div className="week-strip-grid">
                  {upcomingWeekDays.map((day) => (
                    <div
                      key={day.dayKey}
                      className={`week-day-card ${day.isToday ? 'today' : ''} ${day.status}`}
                      onClick={() => setPreviewDay(day)}
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
              </div>
            </div>
          ) : view === 'wardrobe' ? (
            <div className="sw-stack">
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
                        <button className="sw-btn" onClick={() => onWearOutfit(o.id)}>Wear</button>
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
                        <button className="sw-btn" onClick={() => onWearOutfit(o.id)}>Wear</button>
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
                    <button className="sw-btn" onClick={() => setCreateEventModal(true)}>
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
                    <button className="sw-btn accent" onClick={() => setCreateEventModal(true)}>New event</button>
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
                          <button className="sw-btn ghost" onClick={() => { setEditEventData({ id: selectedPlannerEvent.id, name: selectedPlannerEvent.name, type: selectedPlannerEvent.type, location: selectedPlannerEvent.location, startDate: selectedPlannerEvent.startDate.split('T')[0], endDate: selectedPlannerEvent.endDate.split('T')[0], preferredStyles: selectedPlannerEvent.preferredStyles || [] }); setEditEventModal(true); }}>Edit</button>
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
              toggleTheme={toggleTheme}
              onLogout={onLogout}
              onSaveProfile={handleSaveProfile}
              clothes={clothes}
              outfits={outfits}
              aiOutfitCount={aiOutfitCount}
            />
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

