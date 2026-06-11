import React, { useState } from 'react';
import { authApi } from '../services/wardrobeApi';
import { getErrorMessage } from '../utils/errors';
import { evaluatePasswordStrength, meetsPasswordPolicy } from '../utils/passwordStrength';

const AuthPage = ({ onLoginSuccess }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const strength = evaluatePasswordStrength(password);

  const resetFields = () => {
    setError('');
    setPassword('');
    setConfirmPassword('');
  };

  const handleAuth = async (e) => {
    e.preventDefault();
    setError('');

    if (!isLogin) {
      if (!meetsPasswordPolicy(password)) {
        setError('Password must be at least 8 characters and include a letter and a number.');
        return;
      }
      if (password !== confirmPassword) {
        setError('Passwords do not match.');
        return;
      }
    }

    setLoading(true);
    try {
      const res = isLogin
        ? await authApi.login({ email, password })
        : await authApi.register({ username, email, password });

      // Backend returns { token, user }.
      onLoginSuccess(res.data);
    } catch (err) {
      setError(getErrorMessage(err, 'auth failed'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-fullscreen">
      <div className="auth-card">
        <div className="auth-brand">
          <div className="mark">W</div>
          <div className="name">WardrobeManager</div>
        </div>
        <p className="auth-subtitle">{isLogin ? 'sign in to your closet' : 'create your account'}</p>
        <form className="smooth-form" onSubmit={handleAuth}>
          {!isLogin && (
            <input
              type="text"
              placeholder="username"
              value={username}
              onChange={e => setUsername(e.target.value)}
              minLength={3}
              required
            />
          )}
          <input type="email" placeholder="email address" value={email} onChange={e => setEmail(e.target.value)} required />
          <input type="password" placeholder="password" value={password} onChange={e => setPassword(e.target.value)} required />

          {!isLogin && password.length > 0 && (
            <div className="pw-strength">
              <div className="pw-strength-bar">
                {[0, 1, 2, 3].map(i => (
                  <span
                    key={i}
                    className="pw-strength-seg"
                    style={{ background: i < strength.score ? strength.color : 'var(--border-subtle, #ddd)' }}
                  />
                ))}
              </div>
              <span className="pw-strength-label" style={{ color: strength.color }}>{strength.label}</span>
            </div>
          )}

          {!isLogin && (
            <input
              type="password"
              placeholder="confirm password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              required
            />
          )}

          {error && <p className="error-text">{error}</p>}
          <button className="soft-btn" type="submit" disabled={loading}>
            {loading ? 'please wait…' : isLogin ? 'sign in' : 'create account'}
          </button>
        </form>
        <span className="soft-link" onClick={() => { setIsLogin(!isLogin); resetFields(); }}>
          {isLogin ? 'new here? create account' : '← back to sign in'}
        </span>
      </div>
    </div>
  );
};

export default AuthPage;
