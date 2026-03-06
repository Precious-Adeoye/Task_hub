import api from './api';
import { Invitation } from './types';

export const invitations = {
  create: (orgId: string, data: { email: string; role: string }) =>
    api.post<Invitation>(`/organisations/${orgId}/invitations`, data),

  getForOrg: (orgId: string) =>
    api.get<Invitation[]>(`/organisations/${orgId}/invitations`),

  getMyPending: () =>
    api.get<Invitation[]>('/invitations/pending'),

  accept: (id: string) =>
    api.post(`/invitations/${id}/accept`),

  decline: (id: string) =>
    api.post(`/invitations/${id}/decline`),
};
