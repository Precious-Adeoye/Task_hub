import api from './api';
import { AuditEntry } from './types';

export const audit = {
  getLogs: (params?: any) =>
    api.get<{ logs: AuditEntry[]; totalCount: number }>('/audit', { params }),
  getSummary: (params?: any) =>
    api.get('/audit/summary', { params }),
};
