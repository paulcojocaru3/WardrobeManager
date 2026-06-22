import axios from 'axios';
import { API_BASE_URL } from '../config/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
});

// On 401 the auth cookie is missing/expired/invalid; clear the session and send the
// user back to login. A custom event lets App react without a hard reload.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      localStorage.removeItem('wardrobe_user');
      window.dispatchEvent(new Event('wardrobe:unauthorized'));
    }
    return Promise.reject(error);
  }
);
