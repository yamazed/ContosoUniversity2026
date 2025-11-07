import apiClient from './client';
import type { Notification } from '../types/notification';
import type { ApiResponse } from '../types/api';

export const notificationsApi = {
  /**
   * Get all notifications ordered by creation date (newest first)
   */
  getAll: async (): Promise<Notification[]> => {
    const response = await apiClient.get<ApiResponse<Notification[]>>('/notifications');
    console.log('Notifications API response:', response.data);
    
    // Handle the ApiResponse wrapper
    if (response.data && response.data.data) {
      return response.data.data;
    }
    
    // Fallback if response structure is different
    return [];
  },

  /**
   * Get notification details by ID
   */
  getById: async (id: number): Promise<Notification> => {
    const response = await apiClient.get<ApiResponse<Notification>>(`/notifications/${id}`);
    return response.data.data;
  },

  /**
   * Mark a notification as read
   */
  markAsRead: async (id: number): Promise<Notification> => {
    const response = await apiClient.post<ApiResponse<Notification>>(`/notifications/${id}/mark-read`);
    return response.data.data;
  },
};
