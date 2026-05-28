import React, { useState } from 'react';
import './App.css';
import { ThemeProvider } from './contexts/ThemeContext';
import AuthPage from './pages/AuthPage';
import DashboardPage from './pages/DashboardPage';

const getInitialUser = () => {
  const savedUser = localStorage.getItem('wardrobe_user');
  if (!savedUser) {
    return null;
  }

  try {
    return JSON.parse(savedUser);
  } catch {
    localStorage.removeItem('wardrobe_user');
    return null;
  }
};

function App() {
  const [user, setUser] = useState(getInitialUser);
  const isLoggedIn = Boolean(user);

  const handleLoginSuccess = (userData) => {
    localStorage.setItem('wardrobe_user', JSON.stringify(userData));
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem('wardrobe_user');
    setUser(null);
  };

  return (
    <ThemeProvider>
      <div className="app-container">
        {isLoggedIn ? (
          <DashboardPage user={user} onLogout={handleLogout} onUserUpdate={handleLoginSuccess} />
        ) : (
          <AuthPage onLoginSuccess={handleLoginSuccess} />
        )}
      </div>
    </ThemeProvider>
  );
}

export default App;
