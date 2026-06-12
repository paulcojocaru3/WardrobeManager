import React, { useState, useEffect } from 'react';
import './App.css';
import { ThemeProvider } from './contexts/ThemeContext';
import AuthPage from './pages/AuthPage';
import DashboardPage from './pages/DashboardPage';
import { TOKEN_KEY } from './services/apiClient';

const USER_KEY = 'wardrobe_user';

const getInitialUser = () => {
  const savedUser = localStorage.getItem(USER_KEY);
  const token = localStorage.getItem(TOKEN_KEY);
  // A user is only valid alongside a token.
  if (!savedUser || !token) {
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

  // Login/register return { token, user }: persist both.
  const handleAuthSuccess = ({ token, user: userData }) => {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
    setUser(userData);
  };

  // Profile/preference updates return only the user (token unchanged).
  const handleUserUpdate = (userData) => {
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  };

  // The apiClient fires this when a request gets a 401 (expired/invalid token).
  useEffect(() => {
    const onUnauthorized = () => setUser(null);
    window.addEventListener('wardrobe:unauthorized', onUnauthorized);
    return () => window.removeEventListener('wardrobe:unauthorized', onUnauthorized);
  }, []);

  return (
    <ThemeProvider>
      <div className="app-container">
        {isLoggedIn ? (
          <DashboardPage user={user} onLogout={handleLogout} onUserUpdate={handleUserUpdate} />
        ) : (
          <AuthPage onLoginSuccess={handleAuthSuccess} />
        )}
      </div>
    </ThemeProvider>
  );
}

export default App;
