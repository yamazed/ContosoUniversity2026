import { useState, useMemo, useCallback } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Button,
  IconButton,
  Typography,
  Alert,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Toolbar,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  CheckCircle as CheckCircleIcon,
  Circle as CircleIcon,
} from '@mui/icons-material';
import { useNotifications, useMarkNotificationAsRead } from '../../hooks/useNotifications';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
type SortField = 'createdAt' | 'entityType' | 'operation';
type SortDirection = 'asc' | 'desc';

export const NotificationList = () => {
  const [sortField, setSortField] = useState<SortField>('createdAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [filterEntityType, setFilterEntityType] = useState<string>('all');
  const [filterOperation, setFilterOperation] = useState<string>('all');
  const [filterReadStatus, setFilterReadStatus] = useState<string>('all');
  const [autoRefresh, setAutoRefresh] = useState(true); // Enable auto-refresh by default

  // Fetch notifications with optional auto-refresh (every 30 seconds)
  const { data: notifications, isLoading, error, refetch } = useNotifications({
    refetchInterval: autoRefresh ? 30000 : false,
  });

  // Mark notification as read mutation
  const markAsReadMutation = useMarkNotificationAsRead();

  // Get unique entity types and operations for filters
  const { entityTypes, operations } = useMemo(() => {
    if (!notifications) return { entityTypes: [], operations: [] };
    
    const types = new Set<string>();
    const ops = new Set<string>();
    
    notifications.forEach((n) => {
      types.add(n.entityType);
      ops.add(n.operation);
    });
    
    return {
      entityTypes: Array.from(types).sort(),
      operations: Array.from(ops).sort(),
    };
  }, [notifications]);

  // Filter and sort notifications
  const filteredAndSortedNotifications = useMemo(() => {
    if (!notifications) return [];

    let filtered = [...notifications];

    // Apply filters
    if (filterEntityType !== 'all') {
      filtered = filtered.filter((n) => n.entityType === filterEntityType);
    }
    if (filterOperation !== 'all') {
      filtered = filtered.filter((n) => n.operation === filterOperation);
    }
    if (filterReadStatus !== 'all') {
      filtered = filtered.filter((n) => 
        filterReadStatus === 'read' ? n.isRead : !n.isRead
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      let comparison = 0;
      
      switch (sortField) {
        case 'createdAt':
          comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
          break;
        case 'entityType':
          comparison = a.entityType.localeCompare(b.entityType);
          break;
        case 'operation':
          comparison = a.operation.localeCompare(b.operation);
          break;
      }
      
      return sortDirection === 'asc' ? comparison : -comparison;
    });

    return filtered;
  }, [notifications, filterEntityType, filterOperation, filterReadStatus, sortField, sortDirection]);

  // Handle sort change
  const handleSortChange = useCallback((field: SortField) => {
    if (sortField === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  }, [sortField]);

  // Handle manual refresh
  const handleRefresh = useCallback(() => {
    refetch();
  }, [refetch]);

  // Handle toggle auto-refresh
  const handleToggleAutoRefresh = useCallback(() => {
    setAutoRefresh((prev) => !prev);
  }, []);

  // Handle mark as read
  const handleMarkAsRead = useCallback((id: number) => {
    markAsReadMutation.mutate(id);
  }, [markAsReadMutation]);

  // Format date for display
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  // Get operation color
  const getOperationColor = (operation: string): 'success' | 'info' | 'error' | 'warning' => {
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

  if (isLoading) {
    return <LoadingSpinner message="Loading notifications..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading notifications: {error.message}
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Notifications
      </Typography>

      <Paper sx={{ mb: 2 }}>
        <Toolbar sx={{ gap: 2, flexWrap: 'wrap' }}>
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Entity Type</InputLabel>
            <Select
              value={filterEntityType}
              label="Entity Type"
              onChange={(e) => setFilterEntityType(e.target.value)}
            >
              <MenuItem value="all">All Types</MenuItem>
              {entityTypes.map((type) => (
                <MenuItem key={type} value={type}>
                  {type}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Operation</InputLabel>
            <Select
              value={filterOperation}
              label="Operation"
              onChange={(e) => setFilterOperation(e.target.value)}
            >
              <MenuItem value="all">All Operations</MenuItem>
              {operations.map((op) => (
                <MenuItem key={op} value={op}>
                  {op}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={filterReadStatus}
              label="Status"
              onChange={(e) => setFilterReadStatus(e.target.value)}
            >
              <MenuItem value="all">All</MenuItem>
              <MenuItem value="unread">Unread</MenuItem>
              <MenuItem value="read">Read</MenuItem>
            </Select>
          </FormControl>

          <Box sx={{ flexGrow: 1 }} />

          <Tooltip title={autoRefresh ? 'Auto-refresh enabled (30s)' : 'Enable auto-refresh'}>
            <Button
              variant={autoRefresh ? 'contained' : 'outlined'}
              size="small"
              onClick={handleToggleAutoRefresh}
            >
              {autoRefresh ? 'Auto-Refresh ON' : 'Auto-Refresh OFF'}
            </Button>
          </Tooltip>

          <Tooltip title="Refresh now">
            <IconButton
              color="primary"
              onClick={handleRefresh}
              aria-label="Refresh notifications"
            >
              <RefreshIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </Paper>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell width="50">Status</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'entityType'}
                  direction={sortField === 'entityType' ? sortDirection : 'asc'}
                  onClick={() => handleSortChange('entityType')}
                >
                  Entity Type
                </TableSortLabel>
              </TableCell>
              <TableCell>Entity ID</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'operation'}
                  direction={sortField === 'operation' ? sortDirection : 'asc'}
                  onClick={() => handleSortChange('operation')}
                >
                  Operation
                </TableSortLabel>
              </TableCell>
              <TableCell>Message</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'createdAt'}
                  direction={sortField === 'createdAt' ? sortDirection : 'asc'}
                  onClick={() => handleSortChange('createdAt')}
                >
                  Timestamp
                </TableSortLabel>
              </TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredAndSortedNotifications.length > 0 ? (
              filteredAndSortedNotifications.map((notification) => (
                <TableRow 
                  key={notification.id} 
                  hover
                  sx={{ 
                    backgroundColor: notification.isRead ? 'inherit' : 'action.hover',
                  }}
                >
                  <TableCell>
                    {notification.isRead ? (
                      <Tooltip title="Read">
                        <CheckCircleIcon color="success" fontSize="small" />
                      </Tooltip>
                    ) : (
                      <Tooltip title="Unread">
                        <CircleIcon color="primary" fontSize="small" />
                      </Tooltip>
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontWeight={notification.isRead ? 'normal' : 'bold'}>
                      {notification.entityType}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontWeight={notification.isRead ? 'normal' : 'bold'}>
                      {notification.entityId}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={notification.operation}
                      color={getOperationColor(notification.operation)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontWeight={notification.isRead ? 'normal' : 'bold'}>
                      {notification.message}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(notification.createdAt)}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    {!notification.isRead && (
                      <Tooltip title="Mark as read">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => handleMarkAsRead(notification.id)}
                          disabled={markAsReadMutation.isPending}
                          aria-label="Mark as read"
                        >
                          <CheckCircleIcon />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 3 }}>
                    {notifications && notifications.length > 0
                      ? 'No notifications match your filters.'
                      : 'No notifications found.'}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {filteredAndSortedNotifications.length > 0 && (
        <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            Showing {filteredAndSortedNotifications.length} of {notifications?.length || 0} notifications
          </Typography>
        </Box>
      )}
    </Box>
  );
};
