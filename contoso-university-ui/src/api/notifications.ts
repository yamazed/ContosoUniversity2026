import axios from 'axios';
import type { Notification } from '../types/notification';

// Create a separate client for NotificationAPI
const notificationApiClient = axios.create({
  baseURL: import.meta.env.VITE_NOTIFICATION_API_BASE_URL || 'http://localhost:8080/api',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: false,
  timeout: 30000,
});

// Paginated response type
interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    totalCount: number;
    currentPage: number;
    pageSize: number;
    totalPages: number;
  };
}

export const notificationsApi = {
  /**
   * Get all notifications ordered by creation date (newest first)
   */
  getAll: async (): Promise<Notification[]> => {
    const response = await notificationApiClient.get<PaginatedResponse<Notification>>('/notifications');
    console.log('Notifications API response:', response.data);
    
    // Extract data array from paginated response
    return response.data?.data || [];
  },

  /**
   * Get notification details by ID
   */
  getById: async (id: number): Promise<Notification> => {
    const response = await notificationApiClient.get<Notification>(`/notifications/${id}`);
    return response.data;
  },

  /**
   * Mark a notification as read
   */
  markAsRead: async (id: number): Promise<Notification> => {
    const response = await notificationApiClient.put<Notification>(`/notifications/${id}/read`);
    return response.data;
  },
};
