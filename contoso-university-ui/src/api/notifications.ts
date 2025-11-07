import apiClient from './client';
import type { Notification } from '../types/notification';
import type { ApiResponse } from '../types/api';

export const notificationsApi = {
  /**
   * Get all notifications ordered by creation date (newest first)
   */
  getAll: async (): Promise<Notification[]> => {
    const response = await apiClient.get<ApiResponse<Notification[]>>('/notifications');
    return response.data.data;
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
