import React, { useState } from 'react';
import Button from './Button';

const card = {
  background: 'var(--card-bg)',
  border: '1px solid var(--border-subtle)',
  borderRadius: '18px',
  padding: '24px',
};

const label = {
  display: 'block',
  fontSize: '11px',
  letterSpacing: '1px',
  textTransform: 'uppercase',
  opacity: 0.6,
  marginBottom: '6px',
};

const input = {
  width: '100%',
  padding: '11px 14px',
  background: 'var(--bg-subtle)',
  color: 'var(--fg)',
  border: '1px solid var(--border-subtle)',
  borderRadius: '12px',
  fontSize: '14px',
};

const SettingsSection = ({
  userInitials,
  userDisplayName,
  userEmail,
  memberSince,
  city,
  onOpenCityModal,
  isDarkMode,
  toggleTheme,
  onLogout,
  onSaveProfile,
  clothes = [],
  outfits = [],
  aiOutfitCount = 0,
}) => {
  const [username, setUsername] = useState(userDisplayName === 'wardrobe user' ? '' : userDisplayName);
  const [email, setEmail] = useState(userEmail === 'no email' ? '' : userEmail);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState(null); // { type: 'ok' | 'err', text }

  const handleSave = async () => {
    if (!currentPassword) {
      setFeedback({ type: 'err', text: 'Enter your current password to save changes.' });
      return;
    }
    setSaving(true);
    setFeedback(null);
    try {
      await onSaveProfile({
        username: username || null,
        email: email || null,
        newPassword: newPassword || null,
        currentPassword,
      });
      setCurrentPassword('');
      setNewPassword('');
      setFeedback({ type: 'ok', text: 'Profile updated.' });
    } catch (err) {
      const text = err?.response?.data?.error
        || err?.response?.data?.errors?.[0]?.errorMessage
        || 'Could not update profile.';
      setFeedback({ type: 'err', text });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px', maxWidth: '760px' }}>

      {/* Profile header */}
      <div style={{ ...card, display: 'flex', alignItems: 'center', gap: '18px' }}>
        <div style={{
          width: '64px', height: '64px', borderRadius: '50%',
          background: 'var(--bg-subtle)', border: '1px solid var(--border-subtle)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: '24px', fontWeight: 900, flexShrink: 0,
        }}>{userInitials}</div>
        <div style={{ minWidth: 0 }}>
          <h3 style={{ margin: 0, fontSize: '20px', fontWeight: 800 }}>{userDisplayName}</h3>
          <p style={{ margin: '2px 0 0', opacity: 0.6, fontSize: '13px' }}>{userEmail}</p>
          <p style={{ margin: '6px 0 0', opacity: 0.45, fontSize: '11px', letterSpacing: '0.5px' }}>
            MEMBER SINCE {String(memberSince).toUpperCase()}
          </p>
        </div>
      </div>

      {/* Quick stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '14px' }}>
        {[
          { n: clothes.length, t: 'items' },
          { n: outfits.length, t: 'outfits' },
          { n: aiOutfitCount, t: 'ai looks' },
        ].map((s) => (
          <div key={s.t} style={{ ...card, padding: '18px', textAlign: 'center' }}>
            <div style={{ fontSize: '28px', fontWeight: 900 }}>{s.n}</div>
            <div style={{ fontSize: '11px', letterSpacing: '1px', textTransform: 'uppercase', opacity: 0.6 }}>{s.t}</div>
          </div>
        ))}
      </div>

      {/* Preferences */}
      <div style={card}>
        <h3 style={{ margin: '0 0 18px', fontSize: '15px', letterSpacing: '0.5px' }}>Preferences</h3>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px', marginBottom: '14px' }}>
          <div>
            <div style={{ fontWeight: 700 }}>Location</div>
            <div style={{ opacity: 0.6, fontSize: '13px' }}>{city}</div>
          </div>
          <Button label="CHANGE CITY" variant="secondary" onClick={onOpenCityModal} />
        </div>

        <div style={{ height: '1px', background: 'var(--border-subtle)', margin: '14px 0' }} />

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }}>
          <div>
            <div style={{ fontWeight: 700 }}>Theme</div>
            <div style={{ opacity: 0.6, fontSize: '13px' }}>{isDarkMode ? 'Dark mode' : 'Light mode'}</div>
          </div>
          <Button label={isDarkMode ? 'SWITCH TO LIGHT' : 'SWITCH TO DARK'} variant="secondary" onClick={toggleTheme} />
        </div>
      </div>

      {/* Account / edit profile */}
      <div style={card}>
        <h3 style={{ margin: '0 0 18px', fontSize: '15px', letterSpacing: '0.5px' }}>Account</h3>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
          <div>
            <label style={label}>Username</label>
            <input style={input} value={username} onChange={(e) => setUsername(e.target.value)} placeholder="username" />
          </div>
          <div>
            <label style={label}>Email</label>
            <input style={input} type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="email" />
          </div>
          <div>
            <label style={label}>New password</label>
            <input style={input} type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="leave blank to keep" />
          </div>
          <div>
            <label style={label}>Current password *</label>
            <input style={input} type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} placeholder="required to save" />
          </div>
        </div>

        {feedback && (
          <p style={{
            marginTop: '14px', fontSize: '13px', fontWeight: 600,
            color: feedback.type === 'ok' ? '#4caf50' : '#e0564f',
          }}>{feedback.text}</p>
        )}

        <div style={{ marginTop: '18px' }}>
          <Button label="SAVE CHANGES" onClick={handleSave} loading={saving} />
        </div>
      </div>

      {/* Logout */}
      <div style={{ ...card, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }}>
        <div>
          <div style={{ fontWeight: 700 }}>Sign out</div>
          <div style={{ opacity: 0.6, fontSize: '13px' }}>End your session on this device.</div>
        </div>
        <Button label="LOG OUT" variant="secondary" onClick={onLogout} />
      </div>
    </div>
  );
};

export default SettingsSection;
