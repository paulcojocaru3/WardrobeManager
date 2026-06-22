import { useState, useEffect } from 'react';
import Button from './Button';
import { evaluatePasswordStrength, meetsPasswordPolicy } from '../utils/passwordStrength';
import { colorToHex } from '../constants/colors';
import { outfitsApi } from '../services/wardrobeApi';

const card = {
  background: 'var(--card-bg)',
  border: '1px solid var(--border-subtle)',
  borderRadius: '16px',
  padding: '22px 24px',
};

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

// keep preset colors aligned with the app swatches.
const PRESET_COLORS = [
  'black', 'white', 'gray', 'navy', 'blue', 'red',
  'green', 'beige', 'brown', 'pink', 'purple', 'yellow',
];

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
  avoidColors = [],
  outerwearMode = 'auto',
  outerwearTempThreshold = 23,
  varietyLevel = 'normal',
  blockDuplicateUploads = false,
  preferLightOnHotDays = true,
  useGemmaStylistForOutfits = false,
  defaultReuseAfterDays = 3,
  clothes = [],
  outfits = [],
  aiOutfitCount = 0,
}) => {
  const [tab, setTab] = useState('Profile');

  const [username, setUsername] = useState(userDisplayName === 'wardrobe user' ? '' : userDisplayName);
  const [email, setEmail] = useState(userEmail === 'no email' ? '' : userEmail);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState(null);

  const [colors, setColors] = useState(favoriteColors);
  const [colorInput, setColorInput] = useState('');
  const [colorsSaving, setColorsSaving] = useState(false);

  const [owMode, setOwMode] = useState(outerwearMode || 'auto');
  const [owTemp, setOwTemp] = useState(outerwearTempThreshold ?? 23);
  const [owSaving, setOwSaving] = useState(false);

  const [avoid, setAvoid] = useState(avoidColors);
  const [avoidInput, setAvoidInput] = useState('');
  const [avoidSaving, setAvoidSaving] = useState(false);
  const [variety, setVariety] = useState(varietyLevel || 'normal');
  const [blockDupes, setBlockDupes] = useState(blockDuplicateUploads);
  const [lightOnHot, setLightOnHot] = useState(preferLightOnHotDays);
  const [gemmaOnly, setGemmaOnly] = useState(useGemmaStylistForOutfits);
  const [packingReuseDays, setPackingReuseDays] = useState(defaultReuseAfterDays ?? 3);
  const [packingReuseEnabled, setPackingReuseEnabled] = useState(defaultReuseAfterDays !== null);

  useEffect(() => {
    setPackingReuseEnabled(defaultReuseAfterDays !== null);
    if (defaultReuseAfterDays !== null) setPackingReuseDays(defaultReuseAfterDays);
  }, [defaultReuseAfterDays]);

  // load taste insights only when preferences opens.
  const [insights, setInsights] = useState(null);
  useEffect(() => {
    if (tab !== 'Preferences' || insights !== null) return;
    outfitsApi.getLearnedProfile()
      .then((res) => setInsights(res.data))
      .catch(() => setInsights({ topColors: [], topStyles: [], strongPairs: [] }));
  }, [tab, insights]);

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

  const persistAvoid = async (next) => {
    setAvoid(next);
    setAvoidSaving(true);
    try {
      await onSavePreferences({ avoidColors: next });
    } catch {
      /* keep local state */
    } finally {
      setAvoidSaving(false);
    }
  };

  const addAvoid = (c) => {
    const clean = (c || '').trim().toLowerCase();
    if (!clean || avoid.includes(clean)) return;
    persistAvoid([...avoid, clean]);
    setAvoidInput('');
  };

  const removeAvoid = (c) => persistAvoid(avoid.filter((x) => x !== c));

  const saveVariety = (next) => {
    setVariety(next);
    onSavePreferences({ varietyLevel: next }).catch(() => {});
  };

  const saveBlockDupes = (next) => {
    setBlockDupes(next);
    onSavePreferences({ blockDuplicateUploads: next }).catch(() => {});
  };

  const saveLightOnHot = (next) => {
    setLightOnHot(next);
    onSavePreferences({ preferLightOnHotDays: next }).catch(() => {});
  };

  const saveGemmaOnly = (next) => {
    setGemmaOnly(next);
    onSavePreferences({ useGemmaStylistForOutfits: next }).catch(() => {});
  };

  const savePackingReuseEnabled = (enabled) => {
    setPackingReuseEnabled(enabled);
    onSavePreferences({ defaultReuseAfterDays: enabled ? packingReuseDays : null }).catch(() => {});
  };

  const savePackingReuseDays = () => {
    const days = Math.max(2, Math.min(14, Number(packingReuseDays) || 3));
    setPackingReuseDays(days);
    if (packingReuseEnabled) onSavePreferences({ defaultReuseAfterDays: days }).catch(() => {});
  };

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
    <div className="settings-shell">
      <section className="settings-hero">
        <div className="settings-avatar">{userInitials}</div>
        <div className="settings-hero-copy">
          <p className="settings-kicker">Account settings</p>
          <h2>{userDisplayName}</h2>
          <p>{userEmail}</p>
          <span>Member since {memberSince}</span>
        </div>
        <div className="settings-hero-stats">
          {[
            { n: clothes.length, t: 'items' },
            { n: outfits.length, t: 'outfits' },
            { n: aiOutfitCount, t: 'ai looks' },
          ].map((s) => (
            <div key={s.t} className="settings-stat">
              <strong>{s.n}</strong>
              <span>{s.t}</span>
            </div>
          ))}
        </div>
      </section>

      <div className="set-tabs">
        {TABS.map((t) => (
          <button key={t} className={`set-tab ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>{t}</button>
        ))}
      </div>

      {tab === 'Profile' && (
        <div className="settings-grid">
          <div style={card}>
            <h3 style={cardHTight}>Profile summary</h3>
            <p className="settings-copy">
              This account owns your wardrobe, generated outfits, planner events and recommender preferences.
            </p>
            <div className="settings-summary-list">
              <span>Preferred city</span>
              <strong>{city}</strong>
              <span>Theme</span>
              <strong>{isDarkMode ? 'Dark mode' : 'Light mode'}</strong>
              <span>Gemma stylist</span>
              <strong>{gemmaOnly ? 'Enabled' : 'Optional'}</strong>
            </div>
          </div>

          <div style={card}>
            <h3 style={cardHTight}>Quick account actions</h3>
            <p className="settings-copy">Update your city, change the interface theme, or move to security settings for credentials.</p>
            <div className="settings-action-row">
              <Button label="Change city" variant="secondary" onClick={onOpenCityModal} />
              <Button label={isDarkMode ? 'Use light mode' : 'Use dark mode'} variant="secondary" onClick={toggleTheme} />
              <Button label="Security" variant="secondary" onClick={() => setTab('Security')} />
            </div>
          </div>
        </div>
      )}

      {tab === 'Preferences' && (
        <div className="settings-grid">
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
              Colors to avoid {avoidSaving && <span style={{ opacity: 0.5, fontSize: '13px', fontFamily: 'var(--sw-font-mono)' }}>· saving…</span>}
            </h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              Suggestions will gently steer away from these colors.
            </p>

            {avoid.length > 0 && (
              <div className="color-chips">
                {avoid.map((c) => (
                  <span key={c} className="color-chip">
                    <span className="swatch" style={swatchStyle(c)} />
                    {c}
                    <span className="x" onClick={() => removeAvoid(c)} title="Remove">×</span>
                  </span>
                ))}
              </div>
            )}

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginTop: '16px' }}>
              {PRESET_COLORS.filter((n) => !avoid.includes(n)).map((name) => (
                <button key={name} className="color-chip" style={{ cursor: 'pointer' }} onClick={() => addAvoid(name)}>
                  <span className="swatch" style={swatchStyle(name)} />
                  {name}
                  <span style={{ fontWeight: 800, opacity: 0.5 }}>+</span>
                </button>
              ))}
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '16px' }}>
              <input
                style={{ ...input, flex: 1 }}
                value={avoidInput}
                onChange={(e) => setAvoidInput(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addAvoid(avoidInput); } }}
                placeholder="add another color to avoid"
              />
              <Button label="ADD" variant="secondary" onClick={() => addAvoid(avoidInput)} />
            </div>
          </div>

          <div style={card}>
            <h3 style={cardHTight}>Packing strategy</h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              Default rotation for tops and bottoms in new events. Shoes, outerwear and accessories remain reusable every day.
            </p>

            <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer', fontSize: '14px' }}>
              <input
                type="checkbox"
                checked={packingReuseEnabled}
                onChange={(e) => savePackingReuseEnabled(e.target.checked)}
              />
              Reuse tops and bottoms during an event
            </label>

            {packingReuseEnabled && (
              <div style={{ marginTop: '16px', maxWidth: '220px' }}>
                <label style={label}>Reuse after days</label>
                <input
                  style={input}
                  type="number"
                  min="2"
                  max="14"
                  value={packingReuseDays}
                  onChange={(e) => setPackingReuseDays(e.target.value)}
                  onBlur={savePackingReuseDays}
                  onKeyDown={(e) => { if (e.key === 'Enter') e.currentTarget.blur(); }}
                />
              </div>
            )}
          </div>

          <div style={card}>
            <h3 style={cardHTight}>Generation defaults</h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              How outfit suggestions behave by default.
            </p>

            <div>
              <label style={label}>Variety</label>
              <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                {['low', 'normal', 'high'].map((lvl) => (
                  <Button
                    key={lvl}
                    label={lvl.toUpperCase()}
                    variant={variety === lvl ? 'primary' : 'secondary'}
                    onClick={() => saveVariety(lvl)}
                  />
                ))}
              </div>
              <p style={{ margin: '8px 0 0', opacity: 0.55, fontSize: '12px' }}>
                Higher variety spreads wear and avoids repeating recent items.
              </p>
            </div>

            <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer', fontSize: '14px', marginTop: '18px' }}>
              <input
                type="checkbox"
                checked={lightOnHot}
                onChange={(e) => saveLightOnHot(e.target.checked)}
              />
              Lean lighter on hot days
            </label>
            <p style={{ margin: '8px 0 0', opacity: 0.55, fontSize: '12px' }}>
              The warmer it gets, the more suggestions favor short-sleeve tops and shorts.
            </p>

            <div style={{ height: '1px', background: 'var(--border-subtle)', margin: '18px 0' }} />

            <label style={{ display: 'flex', alignItems: 'flex-start', gap: '10px', cursor: 'pointer', fontSize: '14px' }}>
              <input
                type="checkbox"
                checked={gemmaOnly}
                onChange={(e) => saveGemmaOnly(e.target.checked)}
                style={{ marginTop: '2px' }}
              />
              <span>
                Use Gemma3 as the final stylist
                <span style={{ display: 'block', marginTop: '5px', opacity: 0.58, fontSize: '12px', lineHeight: 1.45 }}>
                  FashionCLIP still finds the wardrobe candidates, but Gemma3 chooses the final outfit before it opens. This is slower, but the first result is the styled result.
                </span>
              </span>
            </label>
          </div>

          <div style={card}>
            <h3 style={cardHTight}>Wardrobe uploads</h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              How uploads behave when a near-identical item is detected.
            </p>

            <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer', fontSize: '14px' }}>
              <input
                type="checkbox"
                checked={blockDupes}
                onChange={(e) => saveBlockDupes(e.target.checked)}
              />
              Block duplicate uploads automatically
            </label>
            <p style={{ margin: '8px 0 0', opacity: 0.55, fontSize: '12px' }}>
              When on, an item detected as a duplicate is rejected with a notification instead of asking you to confirm.
            </p>
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

          <div style={card}>
            <h3 style={cardHTight}>How your recommender has adapted</h3>
            <p style={{ margin: '0 0 14px', opacity: 0.6, fontSize: '13px' }}>
              Learned from the outfits you accept, wear and favorite. These gently nudge future suggestions.
            </p>
            {insights === null ? (
              <p style={{ opacity: 0.5, fontSize: '13px', fontFamily: 'var(--sw-font-mono)' }}>loading…</p>
            ) : (insights.topColors?.length || insights.topStyles?.length || insights.avoidedColors?.length || insights.strongPairs?.length) ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                {insights.topColors?.length > 0 && (
                  <div>
                    <label style={label}>Colors you tend to like</label>
                    <div className="color-chips">
                      {insights.topColors.map((c) => (
                        <span key={c.label} className="color-chip">
                          <span className="swatch" style={swatchStyle(c.label)} />
                          {c.label}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
                {insights.avoidedColors?.length > 0 && (
                  <div>
                    <label style={label}>Colors you tend to avoid</label>
                    <div className="color-chips">
                      {insights.avoidedColors.map((c) => (
                        <span key={c.label} className="color-chip" style={{ opacity: 0.6 }}>
                          <span className="swatch" style={swatchStyle(c.label)} />
                          {c.label}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
                {insights.topStyles?.length > 0 && (
                  <div>
                    <label style={label}>Styles you reach for</label>
                    <div style={{ fontSize: '14px' }}>{insights.topStyles.map((s) => s.label).join(' · ')}</div>
                  </div>
                )}
                {insights.strongPairs?.length > 0 && (
                  <div>
                    <label style={label}>Pairings that work for you</label>
                    <ul style={{ margin: 0, paddingLeft: '18px', fontSize: '14px', opacity: 0.85 }}>
                      {insights.strongPairs.map((p, i) => (
                        <li key={i}>{p.itemA} + {p.itemB}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            ) : (
              <p style={{ opacity: 0.6, fontSize: '13px' }}>
                Nothing learned yet — accept, wear and favorite a few AI outfits and your taste will show up here.
              </p>
            )}
          </div>
        </div>
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
