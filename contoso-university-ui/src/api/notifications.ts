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

export const notificationsApi = {
  /**
   * Get all notifications ordered by creation date (newest first)
   */
  getAll: async (): Promise<Notification[]> => {
    const response = await notificationApiClient.get<Notification[]>('/notifications');
    console.log('Notifications API response:', response.data);
    
    // NotificationAPI returns array directly, not wrapped in ApiResponse
    return response.data || [];
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
    const response = await notificationApiClient.post<Notification>(`/notifications/${id}/mark-read`);
    return response.data;
  },
};
