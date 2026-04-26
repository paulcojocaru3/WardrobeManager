import React, { useState } from 'react';
import Button from '../components/Button';
import { authApi } from '../services/wardrobeApi';
import { getErrorMessage } from '../utils/errors';

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

    try {
      const payload = isLogin ? { email, password } : { email, passwordHash: password, username: email.split('@')[0] };

      const res = isLogin
        ? await authApi.login(payload)
        : await authApi.register(payload);

      onLoginSuccess(res.data);
    } catch (err) {
      setError(getErrorMessage(err, 'auth failed'));
    }
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
