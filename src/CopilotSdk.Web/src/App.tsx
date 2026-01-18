import React, { useEffect, useState } from 'react';
import './App.css';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

function App() {
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchWeatherData();
  }, []);

  const fetchWeatherData = async () => {
    try {
      setLoading(true);
      const response = await fetch('https://localhost:5001/weatherforecast');
      if (!response.ok) {
        throw new Error('Failed to fetch weather data');
      }
      const data = await response.json();
      setForecasts(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>Copilot SDK</h1>
        <p>React + .NET Web API Proof of Concept</p>
      </header>
      <main className="App-main">
        <h2>Weather Forecast</h2>
        {loading && <p>Loading...</p>}
        {error && <p className="error">Error: {error}</p>}
        {!loading && !error && (
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Temp (C)</th>
                <th>Temp (F)</th>
                <th>Summary</th>
              </tr>
            </thead>
            <tbody>
              {forecasts.map((forecast, index) => (
                <tr key={index}>
                  <td>{new Date(forecast.date).toLocaleDateString()}</td>
                  <td>{forecast.temperatureC}</td>
                  <td>{forecast.temperatureF}</td>
                  <td>{forecast.summary}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </main>
    </div>
  );
}

export default App;
