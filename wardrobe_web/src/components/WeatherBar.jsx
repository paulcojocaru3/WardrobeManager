import React from 'react';

const WeatherBar = ({ city, weatherInfo, onOpenCityModal, onGenerate, disabled }) => {
  const getWeatherStyles = () => {
    if (!weatherInfo || !weatherInfo.condition) {
      return { background: 'var(--bg-raised)', color: 'var(--fg)' };
    }

    const condition = weatherInfo.condition.toLowerCase();
    if (condition.includes('clear') || condition.includes('sun')) {
      return { background: 'linear-gradient(135deg, #FF8C00 0%, #FFD700 100%)', color: '#000' };
    }

    if (condition.includes('cloud')) {
      return { background: 'linear-gradient(135deg, #757F9A 0%, #D7DDE8 100%)', color: '#000' };
    }

    if (condition.includes('rain') || condition.includes('drizzle')) {
      return { background: 'linear-gradient(135deg, #2c3e50 0%, #4ca1af 100%)', color: '#fff' };
    }

    if (condition.includes('snow')) {
      return { background: 'linear-gradient(135deg, #E0EAFC 0%, #CFDEF3 100%)', color: '#000' };
    }

    return { background: 'linear-gradient(135deg, #646cff 0%, #9089ff 100%)', color: '#fff' };
  };

  const weatherStyles = getWeatherStyles();

  return (
    <div
      className="weather-bar"
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        ...weatherStyles,
        padding: '20px 30px',
        borderRadius: '20px',
        marginBottom: '30px',
        boxShadow: 'var(--shadow-md)',
        color: weatherStyles.color || 'var(--fg)'
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '30px' }}>
        <div style={{ textAlign: 'left', cursor: 'pointer' }} onClick={onOpenCityModal}>
          <span className="robotic-text" style={{ fontSize: '0.65rem', opacity: 0.7, display: 'block', fontWeight: 'bold', color: 'inherit' }}>
            LOCATION (EDIT)
          </span>
          <span style={{ fontSize: '1.4rem', fontWeight: '900', textTransform: 'uppercase', letterSpacing: '1px', borderBottom: '2px solid rgba(0,0,0,0.1)' }}>
            {city}
          </span>
        </div>

        {weatherInfo && (
          <>
            <div style={{ borderLeft: '2px solid rgba(0,0,0,0.1)', paddingLeft: '25px', textAlign: 'left' }}>
              <span style={{ fontSize: '1.4rem', fontWeight: '900' }}>{weatherInfo.temperature.toFixed(0)}°C</span>
              <span style={{ fontSize: '0.9rem', marginLeft: '10px', fontWeight: 'bold', opacity: 0.8, textTransform: 'uppercase' }}>
                {weatherInfo.condition}
              </span>
            </div>

            <div style={{ borderLeft: '2px solid rgba(0,0,0,0.1)', paddingLeft: '25px', textAlign: 'left' }}>
              <span className="robotic-text" style={{ fontSize: '0.65rem', opacity: 0.7, display: 'block', fontWeight: 'bold', color: 'inherit' }}>
                RECOMMENDED SEASON
              </span>
              <span style={{ fontSize: '1.1rem', fontWeight: '900', textTransform: 'uppercase' }}>
                {weatherInfo.seasonSuggestion}
              </span>
            </div>
          </>
        )}
      </div>

      <button
        className="gen-btn"
        onClick={onGenerate}
        disabled={disabled}
        style={{ padding: '12px 30px', fontSize: '0.8rem', background: 'var(--accent-bg)', color: 'var(--accent-fg)', border: 'none', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}
      >
        GENERATE TODAY'S OUTFIT
      </button>
    </div>
  );
};

export default WeatherBar;
