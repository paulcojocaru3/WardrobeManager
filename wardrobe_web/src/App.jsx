import { useState, useEffect } from 'react';
import './App.css';
import { ThemeProvider } from './contexts/ThemeContext';
import { NotificationProvider } from './contexts/NotificationContext';
import Toast from './components/Toast';
import AuthPage from './pages/AuthPage';
import DashboardPage from './pages/DashboardPage';
import { authApi } from './services/wardrobeApi';

const USER_KEY = 'wardrobe_user';

const getInitialUser = () => {
  const savedUser = localStorage.getItem(USER_KEY);
  if (!savedUser) {
    return null;
  }

  try {
    return JSON.parse(savedUser);
  } catch {
    localStorage.removeItem(USER_KEY);
    return null;
  }
};

function App() {
  const [user, setUser] = useState(getInitialUser);
  const isLoggedIn = Boolean(user);

  // Login/register set the HttpOnly auth cookie server-side; persist only the safe user projection.
  const handleAuthSuccess = ({ user: userData }) => {
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
    setUser(userData);
  };

  // Profile/preference updates return only the user (token unchanged).
  const handleUserUpdate = (userData) => {
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
    setUser(userData);
  };

  const handleLogout = () => {
    authApi.logout().catch(() => {});
    localStorage.removeItem(USER_KEY);
    setUser(null);
  };

  // The apiClient fires this when a request gets a 401 (expired/invalid cookie).
  useEffect(() => {
    const onUnauthorized = () => setUser(null);
    window.addEventListener('wardrobe:unauthorized', onUnauthorized);
    return () => window.removeEventListener('wardrobe:unauthorized', onUnauthorized);
  }, []);

  return (
    <ThemeProvider>
      <div className="app-container">
        {isLoggedIn ? (
          <NotificationProvider>
            <DashboardPage user={user} onLogout={handleLogout} onUserUpdate={handleUserUpdate} />
            <Toast />
          </NotificationProvider>
        ) : (
          <AuthPage onLoginSuccess={handleAuthSuccess} />
        )}
      </div>
    </ThemeProvider>
  );
}

export default App;
