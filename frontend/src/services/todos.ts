import api from './api';
import { Todo } from './types';

export const todos = {
  getAll: (params?: any) =>
    api.get<Todo[]>('/todos', { params }),
  getOne: (id: string) =>
    api.get<Todo>(`/todos/${id}`),
  create: (data: any) =>
    api.post<Todo>('/todos', data),
  update: (id: string, data: any, version: string) =>
    api.put(`/todos/${id}`, data, {
      headers: { 'If-Match': `"${version}"` },
    }),
  toggle: (id: string) =>
    api.patch(`/todos/${id}/toggle`),
  softDelete: (id: string) =>
    api.delete(`/todos/${id}/soft`),
  restore: (id: string) =>
    api.post(`/todos/${id}/restore`),
  hardDelete: (id: string) =>
    api.delete(`/todos/${id}`),
};
