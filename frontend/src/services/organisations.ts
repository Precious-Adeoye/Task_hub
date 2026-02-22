import api from './api';
import { Organisation } from './types';

export const organisations = {
  create: (data: { name: string }) =>
    api.post<Organisation>('/organisations', data),
  getMine: () => api.get<Organisation[]>('/organisations'),
  getMembers: (orgId: string) =>
    api.get(`/organisations/${orgId}/members`),
  addMember: (orgId: string, data: { email: string; role: string }) =>
    api.post(`/organisations/${orgId}/members`, data),
};
