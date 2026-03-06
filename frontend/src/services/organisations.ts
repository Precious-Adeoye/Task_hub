import api from './api';
import { Organisation, Member } from './types';

export const organisations = {
  create: (data: { name: string }) =>
    api.post<Organisation>('/organisations', data),
  getMine: () => api.get<Organisation[]>('/organisations'),
  getMembers: (orgId: string) =>
    api.get<Member[]>(`/organisations/${orgId}/members`),
  addMember: (orgId: string, data: { email: string; role: string }) =>
    api.post(`/organisations/${orgId}/members`, data),
};
