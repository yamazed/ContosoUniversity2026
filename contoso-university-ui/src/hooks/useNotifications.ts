import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { notificationsApi } from '../api/notifications';
import type { Notification } from '../types/notification';

// Query keys for cache management
export const notificationKeys = {
  all: ['notifications'] as const,
  lists: () => [...notificationKeys.all, 'list'] as const,
  list: () => [...notificationKeys.lists()] as const,
  details: () => [...notificationKeys.all, 'detail'] as const,
  detail: (id: number) => [...notificationKeys.details(), id] as const,
};

/**
 * Hook to fetch all notifications ordered by creation date (newest first)
 * Supports auto-refresh with refetchInterval option
 */
export const useNotifications = (
  options?: Omit<UseQueryOptions<Notification[]>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: notificationKeys.list(),
    queryFn: () => notificationsApi.getAll(),
    ...options,
  });
};

/**
 * Hook to fetch a single notification by ID
 */
export const useNotification = (
  id: number,
  options?: Omit<UseQueryOptions<Notification>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: notificationKeys.detail(id),
    queryFn: () => notificationsApi.getById(id),
    enabled: !!id, // Only fetch if id is provided
    ...options,
  });
};

/**
 * Hook to mark a notification as read
 * Automatically invalidates the notifications list cache on success
 */
export const useMarkNotificationAsRead = (
  options?: Omit<UseMutationOptions<Notification, Error, number, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Notification, Error, number, unknown>({
    ...options,
    mutationFn: notificationsApi.markAsRead,
    onSuccess: async (data, variables, ...args) => {
      // Invalidate the specific notification detail
      await queryClient.invalidateQueries({ queryKey: notificationKeys.detail(variables) });
      
      // Invalidate all notification lists to refetch with updated data
      await queryClient.invalidateQueries({ queryKey: notificationKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};
