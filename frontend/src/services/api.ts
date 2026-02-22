import axios from 'axios';

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'https://localhost:5001/api/v1',
  withCredentials: true, // Important for cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add organisation ID to requests
api.interceptors.request.use((config) => {
  const orgId = localStorage.getItem('currentOrganisationId');
  if (orgId) {
    config.headers['X-Organisation-Id'] = orgId;
  }
  return config;
});

export default api;
