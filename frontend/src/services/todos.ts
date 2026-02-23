import api from './api';
import { Todo } from './types';

export const todos = {
  getAll: (params?: any) =>
    api.get<Todo[]>('/todo', { params }),
  getOne: (id: string) =>
    api.get<Todo>(`/todo/${id}`),
  create: (data: any) =>
    api.post<Todo>('/todo', data),
  update: (id: string, data: any, version: string) =>
    api.put(`/todo/${id}`, data, {
      headers: { 'If-Match': `"${version}"` },
    }),
  toggle: (id: string) =>
    api.patch(`/todo/${id}/toggle`),
  softDelete: (id: string) =>
    api.delete(`/todo/${id}/soft`),
  restore: (id: string) =>
    api.post(`/todo/${id}/restore`),
  hardDelete: (id: string) =>
    api.delete(`/todo/${id}`),
};
