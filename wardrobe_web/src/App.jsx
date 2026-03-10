import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import './App.css';

const API_BASE_URL = 'http://localhost:5150/api'; 

function App() {
  const [isLogin, setIsLogin] = useState(true);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [user, setUser] = useState(null);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [clothes, setClothes] = useState([]);
  const [selectedItem, setSelectedItem] = useState(null);
  const fileInputRef = useRef(null);

  useEffect(() => {
    if (isLoggedIn && user) fetchClothes();
  }, [isLoggedIn, user]);

  const fetchClothes = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/clothing/${user.id}`);
      setClothes(res.data);
    } catch (err) { console.error(err); }
  };

  const handleAuth = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    const endpoint = isLogin ? 'users/login' : 'users/register';
    try {
      const payload = isLogin ? { email, password } : { email, passwordHash: password, username: email.split('@')[0] };
      const res = await axios.post(`${API_BASE_URL}/${endpoint}`, payload);
      setUser(res.data);
      setIsLoggedIn(true);
    } catch (err) { setError("auth failed"); }
    finally { setLoading(false); }
  };

  const handleFileUpload = async (e) => {
    const file = e.target.files[0];
    if (!file || loading) return;
    setLoading(true);
    const formData = new FormData();
    formData.append('file', file);
    formData.append('userId', user.id);
    try {
      await axios.post(`${API_BASE_URL}/clothing/upload`, formData);
      e.target.value = null;
      fetchClothes(); 
    } catch (err) { alert("ai error"); }
    finally { setLoading(false); }
  };

  const deleteItem = async (id) => {
    try {
      await axios.delete(`${API_BASE_URL}/clothing/${id}`);
      fetchClothes();
    } catch (err) { alert("delete error"); }
  };

  if (isLoggedIn) {
    return (
      <div className="desktop-wrapper">
        <aside className="side-nav">
          <div className="brand">W.</div>
          <button className="exit-circle" onClick={() => setIsLoggedIn(false)}></button>
        </aside>
        <main className="stage">
          <div className="centered-content">
            <h2 className="soft-title">garderoba</h2>
            <div className="clothes-grid">
              {clothes.map(item => (
                <div key={item.id} className="item-card" onClick={() => setSelectedItem(item)}>
                  <button className="delete-trigger" onClick={(e) => { e.stopPropagation(); deleteItem(item.id); }}>remove</button>
                  <img src={item.processedImageUrl} alt="clothing" />
                </div>
              ))}
              <div className={`empty-state-card ${loading ? 'disabled' : ''}`} onClick={() => !loading && fileInputRef.current.click()}>
                <span>{loading ? '...' : '+'}</span>
              </div>
            </div>
            <input type="file" ref={fileInputRef} onChange={handleFileUpload} hidden />
          </div>
        </main>
        {selectedItem && (
          <div className="modal-overlay" onClick={() => setSelectedItem(null)}>
            <div className="modal-content" onClick={e => e.stopPropagation()}>
              <div className="inspect-header">
                <span className="robotic-text">TYPE: {selectedItem.type === 0 ? "TOP" : selectedItem.type === 1 ? "BOTTOM" : selectedItem.type === 2 ? "SHOES" : selectedItem.type === 3 ? "OUTERWEAR" : "ACCESSORY"}</span>
                <span className="robotic-text">COLOR: {selectedItem.color?.toUpperCase()}</span>
              </div>
              <img src={selectedItem.processedImageUrl} alt="large" />
              <button className="close-link" onClick={() => setSelectedItem(null)}>close</button>
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="auth-fullscreen">
      <div className="auth-card">
        <h1 className="minimal-logo">WARDROBE</h1>
        <form className="smooth-form" onSubmit={handleAuth}>
          <input type="email" placeholder="email" value={email} onChange={e => setEmail(e.target.value)} required />
          <input type="password" placeholder="password" value={password} onChange={e => setPassword(e.target.value)} required />
          {error && <p className="error-text">{error}</p>}
          <button type="submit" className="soft-btn" disabled={loading}>{loading ? '...' : (isLogin ? 'login' : 'register')}</button>
        </form>
        <span className="soft-link" onClick={() => setIsLogin(!isLogin)}>{isLogin ? 'new account' : 'back'}</span>
      </div>
    </div>
  );
}

export default App;