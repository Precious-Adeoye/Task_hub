import api from './api';
import { ImportResult } from './types';

export const importExport = {
  exportTodos: (format: 'json' | 'csv') =>
    api.get(`/importexport/export?format=${format}`, { responseType: 'blob' }),

  importTodos: (file: File, format: 'json' | 'csv') => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post<ImportResult>(`/importexport/import?format=${format}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  getTemplate: (format: 'json' | 'csv') =>
    api.get(`/importexport/template?format=${format}`, { responseType: 'blob' }),
};
