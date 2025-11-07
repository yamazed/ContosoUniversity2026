export interface Notification {
  id: number;
  entityType: string;
  entityId: string; // Changed from number to string to match backend DTO
  operation: string;
  message: string;
  createdAt: string;
  createdBy?: string;
  isRead: boolean;
  readAt?: string;
}
