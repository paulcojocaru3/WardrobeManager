import axios from 'axios';
import { API_BASE_URL } from '../config/api';

export const TOKEN_KEY = 'wardrobe_token';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Attach the JWT (if any) to every request.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On 401 the token is missing/expired/invalid → clear the session and send the
// user back to login. A custom event lets App react without a hard reload.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem('wardrobe_user');
      window.dispatchEvent(new Event('wardrobe:unauthorized'));
    }
    return Promise.reject(error);
  }
);
