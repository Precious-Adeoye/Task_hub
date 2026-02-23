import axios from 'axios';

function generateCorrelationId(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5044/api/v1',
  withCredentials: true, // Important for cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add organisation ID and correlation ID to requests
api.interceptors.request.use((config) => {
  const orgId = localStorage.getItem('currentOrganisationId');
  if (orgId) {
    config.headers['X-Organisation-Id'] = orgId;
  }
  config.headers['X-Correlation-Id'] = generateCorrelationId();
  return config;
});

export default api;
