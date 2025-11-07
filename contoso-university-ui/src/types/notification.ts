export interface Notification {
  id: number;
  entityType: string;
  entityId: number;
  operation: string;
  message: string;
  createdAt: string;
  createdBy?: string;
  isRead: boolean;
  readAt?: string;
}
