import React from 'react';

const formatTemp = (value) => (typeof value === 'number' ? `${Math.round(value)}°C` : '—');

const WeatherAlertNotice = ({ alert, locationLabel, onGenerateAlternative, onDismiss }) => {
  if (!alert?.isAvailable || !alert?.isSignificantChange) {
    return null;
  }

  const temperatureDelta = typeof alert.temperatureDelta === 'number' ? alert.temperatureDelta : 0;
  const deltaMagnitude = Math.round(Math.abs(temperatureDelta));
  const deltaDirection = temperatureDelta >= 0 ? 'warmer' : 'colder';
  const storedForecast = alert.storedForecast || null;
  const currentWeather = alert.currentWeather || null;
  const hasAction = typeof onGenerateAlternative === 'function';
  const hasDismiss = typeof onDismiss === 'function';

  const summaryText = deltaMagnitude > 0
    ? `It feels about ${deltaMagnitude}°C ${deltaDirection} than expected.`
    : 'Weather has shifted since you planned this outfit.';

  const recommendation = temperatureDelta < 0
    ? 'Consider warmer layers or a heavier outerwear choice.'
    : 'Consider lighter layers or breathable fabrics.';

  const handleGenerate = () => {
    if (hasAction) {
      onGenerateAlternative();
    }
  };

  const eventName = alert.eventName || 'Planned Event';
  const eventDate = alert.eventDate ? new Date(alert.eventDate).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }) : '';

  return (
    <section
      className="dashboard-section"
      role="alert"
      aria-live="polite"
      aria-atomic="true"
      style={{
        borderColor: 'var(--border-subtle)',
        background: 'var(--card-bg)',
        display: 'flex',
        flexDirection: 'column',
        gap: '14px'
      }}
    >
      <div className="dashboard-section-header" style={{ alignItems: 'center' }}>
        <h3>Weather alert</h3>
        <span>{locationLabel || 'your area'}</span>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <strong style={{ fontSize: '0.9rem', color: 'var(--fg)', letterSpacing: '0.2px' }}>
          Forecast drift detected for {eventName} {eventDate ? `(${eventDate})` : ''}
        </strong>
        <p style={{ margin: 0, fontSize: '0.74rem', color: 'var(--fg-subtle)', lineHeight: 1.6 }}>
          {summaryText} {recommendation}
        </p>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '10px'
        }}
      >
        <div
          style={{
            border: '1px solid var(--border-subtle)',
            borderRadius: '12px',
            padding: '10px',
            background: 'var(--bg-raised)',
            display: 'flex',
            flexDirection: 'column',
            gap: '6px'
          }}
        >
          <span style={{ fontSize: '0.58rem', textTransform: 'uppercase', letterSpacing: '1px', color: 'var(--fg-muted)' }}>
            Planned forecast
          </span>
          <strong style={{ fontSize: '0.9rem', color: 'var(--fg)' }}>
            {formatTemp(storedForecast?.temperature)} • {storedForecast?.condition || 'unknown'}
          </strong>
          <span style={{ fontSize: '0.62rem', color: 'var(--fg-subtle)' }}>
            Season: {storedForecast?.seasonSuggestion || 'n/a'}
          </span>
        </div>
        <div
          style={{
            border: '1px solid var(--border-subtle)',
            borderRadius: '12px',
            padding: '10px',
            background: 'var(--bg-raised)',
            display: 'flex',
            flexDirection: 'column',
            gap: '6px'
          }}
        >
          <span style={{ fontSize: '0.58rem', textTransform: 'uppercase', letterSpacing: '1px', color: 'var(--fg-muted)' }}>
            Current weather
          </span>
          <strong style={{ fontSize: '0.9rem', color: 'var(--fg)' }}>
            {formatTemp(currentWeather?.temperature)} • {currentWeather?.condition || 'unknown'}
          </strong>
          <span style={{ fontSize: '0.62rem', color: 'var(--fg-subtle)' }}>
            Season: {currentWeather?.seasonSuggestion || 'n/a'}
          </span>
        </div>
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px' }}>
        {hasAction && (
          <button
            type="button"
            className="hero-primary-action"
            onClick={handleGenerate}
            aria-label="Generate an alternative outfit based on updated weather"
          >
            Generate alternative
          </button>
        )}
        {hasDismiss && (
          <button
            type="button"
            className="hero-secondary-action"
            onClick={onDismiss}
            aria-label="Dismiss weather alert"
          >
            Dismiss
          </button>
        )}
      </div>
    </section>
  );
};

export default WeatherAlertNotice;
