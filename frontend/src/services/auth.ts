import api from './api';
import { User } from './types';

export const auth = {
  register: (data: { username: string; email: string; password: string }) =>
    api.post<User>('/auth/register', data),
  login: (data: { username: string; password: string }) =>
    api.post<User>('/auth/login', data),
  logout: () => api.post('/auth/logout'),
  me: () => api.get<User>('/auth/me'),
};
