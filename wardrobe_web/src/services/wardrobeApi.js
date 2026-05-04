import axios from 'axios';
import { apiClient } from './apiClient';

export const authApi = {
  login: (payload) => apiClient.post('/users/login', payload),
  register: (payload) => apiClient.post('/users/register', payload),
};

export const outfitsApi = {
  searchCities: (query) => apiClient.get('/outfits/cities/search', { params: { query } }),
  getWeather: (city) => apiClient.get(`/outfits/weather/${city}`),
  getForecast: (city, startDate) => apiClient.get(`/outfits/weather/${city}/forecast`, { params: { startDate } }),
  getByUser: (userId) => apiClient.get(`/outfits/user/${userId}`),
  generateAi: (payload) => apiClient.post('/outfits/generate-ai', payload),
  create: (payload) => apiClient.post('/outfits', payload),
  update: (id, payload) => apiClient.put(`/outfits/${id}`, payload),
  remove: (id) => apiClient.delete(`/outfits/${id}`),
  recordWear: (outfitId, payload) => apiClient.post(`/wear-events/outfit/${outfitId}`, payload),
};

export const clothingApi = {
  getByUser: (userId) => apiClient.get(`/clothing/${userId}`),
  process: (formData) => apiClient.post('/clothing/process', formData),
  add: (payload) => apiClient.post('/clothing/add', payload),
  update: (id, payload) => apiClient.put(`/clothing/${id}`, payload),
  remove: (id) => apiClient.delete(`/clothing/${id}`),
};

export const statsApi = {
  getWearStats: (userId, params) => apiClient.get(`/wear-events/stats/${userId}`, { params }),
};

export const plannerEventsApi = {
  getByUser: (userId) => apiClient.get(`/planner-events/${userId}`),
  getArchivedByUser: (userId) => apiClient.get(`/planner-events/${userId}/archived`),
  create: (payload) => apiClient.post('/planner-events', payload),
  update: (plannerEventId, payload) => apiClient.put(`/planner-events/${plannerEventId}`, payload),
  remove: (userId, plannerEventId) => apiClient.delete(`/planner-events/${userId}/${plannerEventId}`),
  archiveEvent: (eventId) => apiClient.post(`/planner-events/${eventId}/archive`, {}),
  addItinerary: (plannerEventId, payload) => apiClient.post(`/planner-events/${plannerEventId}/itineraries`, payload),
  updateItinerary: (plannerEventId, itineraryId, payload) => apiClient.put(`/planner-events/${plannerEventId}/itineraries/${itineraryId}`, payload),
  removeItinerary: (userId, plannerEventId, itineraryId) => apiClient.delete(`/planner-events/${userId}/${plannerEventId}/itineraries/${itineraryId}`),
  generateOutfits: (plannerEventId, payload) => apiClient.post(`/planner-events/${plannerEventId}/generate-outfits`, payload),
  regenerateItinerary: (plannerEventId, itineraryId, payload) => apiClient.post(`/planner-events/${plannerEventId}/itineraries/${itineraryId}/regenerate`, payload),
};

export const geoApi = {
  detectPrimary: () => axios.get('https://ipapi.co/json/'),
  detectFallback: () => axios.get('http://ip-api.com/json/'),
};
