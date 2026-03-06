export interface User {
  id: string;
  username: string;
  email: string;
  organisations: AuthOrg[];
  pendingInvitationCount: number;
}

export interface AuthOrg {
  id: string;
  name: string;
  role: string;
}

export interface Organisation {
  id: string;
  name: string;
  createdAt: string;
}

export interface Member {
  userId: string;
  username: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface Todo {
  id: string;
  title: string;
  description?: string;
  status: 'Open' | 'Done';
  priority: 'Low' | 'Medium' | 'High';
  tags: string[];
  dueDate?: string;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  assignedTo?: string;
  assignedToUsername?: string;
  version: string;
}

export interface Invitation {
  id: string;
  organisationId: string;
  organisationName: string;
  email: string;
  role: string;
  invitedByUsername: string;
  createdAt: string;
  status: string;
  respondedAt?: string;
}

export interface AuditEntry {
  id: string;
  timestamp: string;
  actorUserId: string;
  actionType: string;
  entityType: string;
  entityId: string;
  details: string;
  correlationId: string;
}

export interface ImportResult {
  acceptedCount: number;
  rejectedCount: number;
  errors: Array<{
    rowNumber: number;
    clientProvidedId?: string;
    errorMessage: string;
  }>;
}
