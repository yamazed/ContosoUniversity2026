import { useEffect, useRef, useState } from 'react';
import { Snackbar, Alert, AlertTitle, IconButton, Box } from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import { useNotifications } from '../../hooks/useNotifications';
import type { Notification } from '../../types/notification';

/**
 * Global notification toast component that polls for new notifications
 * and displays them as toast messages across the entire application
 * 
 * Note: Since notifications come from a queue (not a database), each notification
 * is only returned once and then consumed. We show all notifications from each poll.
 */
export const NotificationToast = () => {
  const displayedNotificationsRef = useRef<Set<number>>(new Set());
  const [openToasts, setOpenToasts] = useState<Notification[]>([]);
  const lastFetchTimeRef = useRef<number>(0);

  // Poll for notifications every 10 seconds
  // Disable caching since notifications come from a queue and are consumed
  const { data: notifications, dataUpdatedAt } = useNotifications({
    refetchInterval: 10000, // Poll every 10 seconds
    refetchIntervalInBackground: true, // Continue polling even when tab is not focused
    staleTime: 0, // Always consider data stale
    gcTime: 0, // Don't cache data (previously cacheTime)
  });

  useEffect(() => {
    console.log('NotificationToast effect triggered:', {
      notifications,
      notificationsLength: notifications?.length,
      dataUpdatedAt,
      lastFetchTime: lastFetchTimeRef.current,
    });

    // Skip if this is the same fetch we already processed
    if (dataUpdatedAt === lastFetchTimeRef.current) {
      console.log('Same fetch time, skipping');
      return;
    }

    // Update last fetch time
    lastFetchTimeRef.current = dataUpdatedAt;

    if (!notifications || notifications.length === 0) {
      console.log('No notifications to display');
      return;
    }

    // Filter out notifications we've already displayed
    const newNotifications = notifications.filter(
      (notification) => !displayedNotificationsRef.current.has(notification.id)
    );

    console.log('Filtered new notifications:', {
      total: notifications.length,
      new: newNotifications.length,
      displayed: Array.from(displayedNotificationsRef.current),
      notificationIds: notifications.map(n => n.id),
    });

    if (newNotifications.length > 0) {
      console.log('Showing new notifications as toasts:', newNotifications);

      // Add new notifications to displayed set
      newNotifications.forEach((n) => displayedNotificationsRef.current.add(n.id));

      // Show toasts for new notifications (limit to 3 at a time)
      setOpenToasts((prev) => {
        const updated = [...newNotifications.slice(0, 3), ...prev].slice(0, 3);
        console.log('Updated openToasts:', updated);
        return updated;
      });
    }
  }, [notifications, dataUpdatedAt]);

  const handleClose = (notificationId: number) => {
    setOpenToasts((prev) => prev.filter((n) => n.id !== notificationId));
  };

  const getOperationSeverity = (operation: string): 'success' | 'info' | 'error' | 'warning' => {
    switch (operation.toLowerCase()) {
      case 'created':
        return 'success';
      case 'updated':
        return 'info';
      case 'deleted':
        return 'error';
      default:
        return 'warning';
    }
  };

  return (
    <Box>

      {openToasts.map((notification, index) => (
        <Snackbar
          key={notification.id}
          open={true}
          autoHideDuration={6000}
          onClose={() => handleClose(notification.id)}
          anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
          sx={{
            top: `${80 + index * 80}px !important`, // Stack toasts vertically
          }}
        >
          <Alert
            severity={getOperationSeverity(notification.operation)}
            variant="filled"
            onClose={() => handleClose(notification.id)}
            action={
              <IconButton
                size="small"
                aria-label="close"
                color="inherit"
                onClick={() => handleClose(notification.id)}
              >
                <CloseIcon fontSize="small" />
              </IconButton>
            }
            sx={{ minWidth: 300 }}
          >
            <AlertTitle>
              {notification.entityType} {notification.operation}
            </AlertTitle>
            {notification.message}
          </Alert>
        </Snackbar>
      ))}
    </Box>
  );
};
