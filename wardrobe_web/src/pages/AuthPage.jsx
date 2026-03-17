import React, { useState } from 'react';
import axios from 'axios';
import Button from '../components/Button';

const API_BASE_URL = 'http://localhost:5150/api'; 

const AuthPage = ({ onLoginSuccess }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleAuth = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    const endpoint = isLogin ? 'users/login' : 'users/register';
    try {
      const payload = isLogin ? { email, password } : { email, passwordHash: password, username: email.split('@')[0] };
      const res = await axios.post(`${API_BASE_URL}/${endpoint}`, payload);
      onLoginSuccess(res.data);
    } catch (err) { setError("auth failed"); }
    finally { setLoading(false); }
  };

  return (
    <div className="auth-fullscreen">
      <div className="auth-card">
        <h1 className="minimal-logo">WARDROBE</h1>
        <form className="smooth-form" onSubmit={handleAuth}>
          <input type="email" placeholder="email" value={email} onChange={e => setEmail(e.target.value)} required />
          <input type="password" placeholder="password" value={password} onChange={e => setPassword(e.target.value)} required />
          {error && <p className="error-text">{error}</p>}
          <Button label={isLogin ? 'login' : 'register'} type="submit" loading={loading} />
        </form>
        <span className="soft-link" onClick={() => setIsLogin(!isLogin)}>{isLogin ? 'new account' : 'back'}</span>
      </div>
    </div>
  );
};

export default AuthPage;
