import React, { useState } from 'react';
import Button from './Button';
import { evaluatePasswordStrength, meetsPasswordPolicy } from '../utils/passwordStrength';
import { colorToHex } from '../constants/colors';

const card = {
  background: 'var(--card-bg)',
  border: '1px solid var(--border-subtle)',
  borderRadius: '16px',
  padding: '22px 24px',
};

// Editorial Air: serif card titles. cardHTight for titles followed by a subtitle line.
const cardH = {
  margin: '0 0 16px',
  fontFamily: 'var(--sw-font-serif)',
  fontWeight: 400,
  fontSize: '22px',
  letterSpacing: '-0.01em',
  lineHeight: 1.1,
  color: 'var(--fg)',
};
const cardHTight = { ...cardH, margin: '0 0 4px' };

const label = {
  display: 'block',
  fontFamily: 'var(--sw-font-mono)',
  fontSize: '10px',
  letterSpacing: '0.14em',
  textTransform: 'uppercase',
  color: 'var(--fg-muted)',
  marginBottom: '7px',
};

const input = {
  width: '100%',
  padding: '11px 14px',
  background: 'var(--bg-soft)',
  color: 'var(--fg)',
  border: '1px solid var(--border-subtle)',
  borderRadius: '12px',
  fontSize: '14px',
};

// A small curated palette; users can also type any color word. Swatch hexes come
// from the canonical colorToHex resolver so they match the rest of the app.
const PRESET_COLORS = [
  'black', 'white', 'gray', 'navy', 'blue', 'red',
  'green', 'beige', 'brown', 'pink', 'purple', 'yellow',
];

// Style for a color swatch: resolved hex, or a neutral ring when unknown.
const swatchStyle = (name) => {
  const hex = colorToHex(name);
  return hex
    ? { background: hex }
    : { background: 'transparent', boxShadow: 'inset 0 0 0 1px var(--border-subtle, #999)' };
};

const TABS = ['Profile', 'Preferences', 'Security', 'Account'];

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
  onSavePreferences,
  onDeleteAccount,
  favoriteColors = [],
  outerwearMode = 'auto',
  outerwearTempThreshold = 23,
  clothes = [],
  outfits = [],
  aiOutfitCount = 0,
}) => {
  const [tab, setTab] = useState('Profile');

  // Security form
  const [username, setUsername] = useState(userDisplayName === 'wardrobe user' ? '' : userDisplayName);
  const [email, setEmail] = useState(userEmail === 'no email' ? '' : userEmail);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState(null);

  // Favorite colors
  const [colors, setColors] = useState(favoriteColors);
  const [colorInput, setColorInput] = useState('');
  const [colorsSaving, setColorsSaving] = useState(false);

  // Outerwear policy
  const [owMode, setOwMode] = useState(outerwearMode || 'auto');
  const [owTemp, setOwTemp] = useState(outerwearTempThreshold ?? 23);
  const [owSaving, setOwSaving] = useState(false);

  // Delete account
  const [confirmName, setConfirmName] = useState('');
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState(null);

  const strength = evaluatePasswordStrength(newPassword);

  const handleSave = async () => {
    if (!currentPassword) {
      setFeedback({ type: 'err', text: 'Enter your current password to save changes.' });
      return;
    }
    if (newPassword && !meetsPasswordPolicy(newPassword)) {
      setFeedback({ type: 'err', text: 'New password must be at least 8 characters and include a letter and a number.' });
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

  const persistColors = async (next) => {
    setColors(next);
    setColorsSaving(true);
    try {
      await onSavePreferences({ favoriteColors: next });
    } catch {
      /* keep local state; surfaced via no toast for simplicity */
    } finally {
      setColorsSaving(false);
    }
  };

  const addColor = (c) => {
    const clean = (c || '').trim().toLowerCase();
    if (!clean || colors.includes(clean)) return;
    persistColors([...colors, clean]);
    setColorInput('');
  };

  const removeColor = (c) => persistColors(colors.filter((x) => x !== c));

  const saveOuterwear = async (payload) => {
    setOwSaving(true);
    try {
      await onSavePreferences(payload);
    } catch {
      /* keep local state */
    } finally {
      setOwSaving(false);
    }
  };

  const selectOwMode = (mode) => {
    setOwMode(mode);
    saveOuterwear({ outerwearMode: mode });
  };

  const OW_MODES = [
    { id: 'auto', label: 'By weather' },
    { id: 'always', label: 'Always' },
    { id: 'never', label: 'Never' },
  ];

  const handleDelete = async () => {
    if (confirmName !== userDisplayName) {
      setDeleteError('Type your username exactly to confirm.');
      return;
    }
    setDeleting(true);
    setDeleteError(null);
    try {
      await onDeleteAccount();
    } catch (err) {
      setDeleteError(err?.response?.data?.error || 'Could not delete account.');
      setDeleting(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', maxWidth: '760px', width: '100%', margin: '0 auto' }}>
      <div className="set-tabs">
        {TABS.map((t) => (
          <button key={t} className={`set-tab ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>{t}</button>
        ))}
      </div>

      {tab === 'Profile' && (
        <>
          <div style={{ ...card, display: 'flex', alignItems: 'center', gap: '18px' }}>
            <div style={{
              width: '60px', height: '60px', borderRadius: '50%',
              background: 'var(--bg-soft)', border: '1px solid var(--border-subtle)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontFamily: 'var(--sw-font-serif)', fontStyle: 'italic', fontSize: '24px',
              color: 'var(--accent)', flexShrink: 0,
            }}>{userInitials}</div>
            <div style={{ minWidth: 0 }}>
              <h3 style={{ margin: 0, fontFamily: 'var(--sw-font-serif)', fontWeight: 400, fontSize: '26px', letterSpacing: '-0.01em', color: 'var(--fg)' }}>{userDisplayName}</h3>
              <p style={{ margin: '3px 0 0', color: 'var(--fg-subtle)', fontSize: '13px' }}>{userEmail}</p>
              <p style={{ margin: '8px 0 0', fontFamily: 'var(--sw-font-mono)', fontSize: '10px', letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--fg-muted)' }}>
                Member since {memberSince}
              </p>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '14px' }}>
            {[
              { n: clothes.length, t: 'items' },
              { n: outfits.length, t: 'outfits' },
              { n: aiOutfitCount, t: 'ai looks' },
            ].map((s) => (
              <div key={s.t} style={{ ...card, padding: '18px' }}>
                <div style={{ fontSize: '30px', fontWeight: 300, letterSpacing: '-0.02em', color: 'var(--fg)', lineHeight: 1 }}>{s.n}</div>
                <div style={{ marginTop: '8px', fontFamily: 'var(--sw-font-mono)', fontSize: '10px', letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--fg-muted)' }}>{s.t}</div>
              </div>
            ))}
          </div>
        </>
      )}

      {tab === 'Preferences' && (
        <>
          <div style={card}>
            <h3 style={cardH}>Location &amp; theme</h3>

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

          <div style={card}>
            <h3 style={cardHTight}>
              Favorite colors {colorsSaving && <span style={{ opacity: 0.5, fontSize: '13px', fontFamily: 'var(--sw-font-mono)' }}>· saving…</span>}
            </h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              Used to nudge outfit suggestions toward colors you love.
            </p>

            {colors.length > 0 && (
              <div className="color-chips">
                {colors.map((c) => (
                  <span key={c} className="color-chip">
                    <span className="swatch" style={swatchStyle(c)} />
                    {c}
                    <span className="x" onClick={() => removeColor(c)} title="Remove">×</span>
                  </span>
                ))}
              </div>
            )}

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginTop: '16px' }}>
              {PRESET_COLORS.filter((n) => !colors.includes(n)).map((name) => (
                <button key={name} className="color-chip" style={{ cursor: 'pointer' }} onClick={() => addColor(name)}>
                  <span className="swatch" style={swatchStyle(name)} />
                  {name}
                  <span style={{ fontWeight: 800, opacity: 0.5 }}>+</span>
                </button>
              ))}
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '16px' }}>
              <input
                style={{ ...input, flex: 1 }}
                value={colorInput}
                onChange={(e) => setColorInput(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addColor(colorInput); } }}
                placeholder="add another color (e.g. olive)"
              />
              <Button label="ADD" variant="secondary" onClick={() => addColor(colorInput)} />
            </div>
          </div>

          <div style={card}>
            <h3 style={cardHTight}>
              Outerwear {owSaving && <span style={{ opacity: 0.5, fontSize: '13px', fontFamily: 'var(--sw-font-mono)' }}>· saving…</span>}
            </h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              When generated outfits should include a jacket or coat.
            </p>

            <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
              {OW_MODES.map((m) => (
                <Button
                  key={m.id}
                  label={m.label}
                  variant={owMode === m.id ? 'primary' : 'secondary'}
                  onClick={() => selectOwMode(m.id)}
                />
              ))}
            </div>

            {owMode === 'auto' && (
              <div style={{ marginTop: '18px' }}>
                <label style={label}>No outerwear above {owTemp}°C</label>
                <input
                  type="range"
                  min={5}
                  max={30}
                  value={owTemp}
                  style={{ width: '100%' }}
                  onChange={(e) => setOwTemp(Number(e.target.value))}
                  onMouseUp={(e) => saveOuterwear({ outerwearTempThreshold: Number(e.target.value) })}
                  onTouchEnd={(e) => saveOuterwear({ outerwearTempThreshold: Number(e.target.value) })}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', opacity: 0.5, fontSize: '12px' }}>
                  <span>5°C</span>
                  <span>30°C</span>
                </div>
              </div>
            )}
          </div>
        </>
      )}

      {tab === 'Security' && (
        <div style={card}>
          <h3 style={cardHTight}>Change credentials</h3>
          <p style={{ margin: '0 0 18px', opacity: 0.6, fontSize: '13px' }}>
            Your current password is required to change any of these.
          </p>

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
              {newPassword.length > 0 && (
                <div className="pw-strength" style={{ marginTop: '8px' }}>
                  <div className="pw-strength-bar">
                    {[0, 1, 2, 3].map((i) => (
                      <span key={i} className="pw-strength-seg" style={{ background: i < strength.score ? strength.color : 'var(--border-subtle)' }} />
                    ))}
                  </div>
                  <span className="pw-strength-label" style={{ color: strength.color }}>{strength.label}</span>
                </div>
              )}
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
      )}

      {tab === 'Account' && (
        <>
          <div style={{ ...card, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }}>
            <div>
              <div style={{ fontWeight: 700 }}>Sign out</div>
              <div style={{ opacity: 0.6, fontSize: '13px' }}>End your session on this device.</div>
            </div>
            <Button label="LOG OUT" variant="secondary" onClick={onLogout} />
          </div>

          <div style={{ ...card }} className="danger-card">
            <h3 style={{ ...cardHTight, color: '#e0564f' }}>Delete account</h3>
            <p style={{ margin: '0 0 14px', opacity: 0.7, fontSize: '13px' }}>
              This permanently removes your account and all your items, outfits and plans. This cannot be undone.
              Type <strong>{userDisplayName}</strong> to confirm.
            </p>
            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
              <input
                style={{ ...input, flex: 1, minWidth: '220px' }}
                value={confirmName}
                onChange={(e) => setConfirmName(e.target.value)}
                placeholder="type your username"
              />
              <button
                className="danger-btn"
                onClick={handleDelete}
                disabled={deleting || confirmName !== userDisplayName}
              >
                {deleting ? 'DELETING…' : 'DELETE ACCOUNT'}
              </button>
            </div>
            {deleteError && (
              <p style={{ marginTop: '12px', fontSize: '13px', fontWeight: 600, color: '#e0564f' }}>{deleteError}</p>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default SettingsSection;
