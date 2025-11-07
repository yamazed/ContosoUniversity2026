# Error Handling and Loading States Guide

This document describes the comprehensive error handling and loading state management implemented in the Contoso University React application.

## Overview

The application implements a multi-layered error handling strategy that provides:
- User-friendly error messages
- Toast notifications for success and error feedback
- Loading spinners during async operations
- Error boundaries to catch React component errors
- Graceful network error handling
- Retry mechanisms for failed requests

## Components

### 1. Toast Notifications (`ToastContext`)

**Location:** `src/contexts/ToastContext.tsx`

A context provider that manages toast notifications throughout the application.

**Usage:**
```typescript
import { useToast } from '../contexts/ToastContext';

const { showSuccess, showError, showWarning, showInfo } = useToast();

// Show success message
showSuccess('Student created successfully!');

// Show error message
showError('Failed to load data');
```

**Features:**
- Multiple toast messages can be displayed simultaneously
- Auto-dismiss after 6 seconds
- Stacked positioning for multiple toasts
- Material-UI Alert component for consistent styling

### 2. Error Display Component

**Location:** `src/components/common/ErrorDisplay.tsx`

A reusable component for displaying error messages with optional retry functionality.

**Usage:**
```typescript
import { ErrorDisplay } from '../../components/common';

<ErrorDisplay 
  error={error} 
  title="Failed to Load Data"
  onRetry={() => refetch()}
/>
```

**Features:**
- Displays error message from Error, ApiError, or string
- Shows detailed error list if available (validation errors)
- Optional retry button
- Consistent Material-UI Alert styling

### 3. Loading Spinner Component

**Location:** `src/components/common/LoadingSpinner.tsx`

A reusable loading indicator component.

**Usage:**
```typescript
import { LoadingSpinner } from '../../components/common';

if (isLoading) {
  return <LoadingSpinner message="Loading students..." />;
}
```

**Features:**
- Customizable message
- Customizable size
- Centered layout with minimum height
- Material-UI CircularProgress

### 4. Error Boundary Component

**Location:** `src/components/common/ErrorBoundary.tsx`

A React error boundary that catches JavaScript errors in child components.

**Usage:**
```typescript
import { ErrorBoundary } from './components/common';

<ErrorBoundary>
  <YourComponent />
</ErrorBoundary>
```

**Features:**
- Catches unhandled React errors
- Displays user-friendly error page
- Shows error details in development
- Provides "Try Again" and "Go to Home" buttons
- Logs errors to console

### 5. Enhanced API Client

**Location:** `src/api/client.ts`

An Axios instance with comprehensive error handling and user-friendly error messages.

**Features:**
- Custom `ApiError` class with status code and error details
- 30-second request timeout
- Automatic retry logic (configured in React Query)
- User-friendly error messages for common HTTP status codes:
  - 400: Invalid request
  - 401: Authentication required (redirects to login)
  - 403: Permission denied
  - 404: Resource not found
  - 409: Concurrency conflict
  - 500: Server error
- Network error handling (timeout, connection issues)
- Detailed error logging

## Implementation Patterns

### Page-Level Error Handling

All pages follow this pattern:

```typescript
export const StudentList = () => {
  const { data, isLoading, error, refetch } = useStudents(params);

  // Show loading spinner
  if (isLoading) {
    return <LoadingSpinner message="Loading students..." />;
  }

  // Show error with retry option
  if (error) {
    return <ErrorDisplay error={error} title="Failed to Load Students" onRetry={() => refetch()} />;
  }

  // Render data
  return <div>{/* ... */}</div>;
};
```

### Mutation Error Handling

Mutations (create, update, delete) use toast notifications:

```typescript
export const StudentCreate = () => {
  const { showSuccess, showError } = useToast();

  const createStudent = useCreateStudent({
    onSuccess: () => {
      showSuccess('Student created successfully!');
      navigate('/students');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to create student');
    },
  });

  return (
    <>
      {createStudent.isError && (
        <ErrorDisplay 
          error={createStudent.error} 
          title="Failed to Create Student"
          onRetry={() => createStudent.reset()}
        />
      )}
      {/* Form */}
    </>
  );
};
```

### React Query Configuration

**Location:** `src/App.tsx`

```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: (failureCount, error: any) => {
        // Don't retry on 4xx errors (client errors)
        if (error?.statusCode && error.statusCode >= 400 && error.statusCode < 500) {
          return false;
        }
        // Retry up to 2 times for other errors
        return failureCount < 2;
      },
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
    mutations: {
      retry: false, // Don't retry mutations by default
    },
  },
});
```

## Error Boundary Placement

Error boundaries are placed at multiple levels:

1. **App Level** (`App.tsx`): Catches all unhandled errors
2. **Layout Level** (`Layout.tsx`): Catches errors in page content
3. **Router Level** (`router.tsx`): Catches routing errors with `errorElement`

This multi-level approach ensures errors are caught and displayed appropriately without crashing the entire application.

## Best Practices

### 1. Always Show Loading States
```typescript
if (isLoading) {
  return <LoadingSpinner message="Loading..." />;
}
```

### 2. Provide Retry Mechanisms
```typescript
<ErrorDisplay 
  error={error} 
  onRetry={() => refetch()}
/>
```

### 3. Use Toast Notifications for Actions
```typescript
showSuccess('Operation completed successfully!');
showError('Operation failed');
```

### 4. Display Inline Errors for Forms
```typescript
{mutation.isError && (
  <ErrorDisplay error={mutation.error} />
)}
```

### 5. Handle Network Errors Gracefully
The API client automatically handles:
- Connection timeouts
- Network unavailability
- Server errors

### 6. Provide User-Friendly Messages
Instead of technical error messages, show:
- "Unable to reach the server. Please check your internet connection."
- "The requested resource was not found."
- "You do not have permission to perform this action."

## Testing Error Scenarios

### Simulate Network Errors
1. Disconnect from network
2. Stop the backend API
3. Use browser DevTools to throttle network

### Simulate Server Errors
1. Modify API to return 500 errors
2. Test with invalid data (400 errors)
3. Test with unauthorized access (401/403 errors)

### Test Error Boundaries
1. Throw errors in components
2. Verify error boundary catches and displays error page
3. Test "Try Again" functionality

## Future Enhancements

1. **Error Tracking Service**: Integrate with Sentry or similar service
2. **Offline Support**: Add service worker for offline functionality
3. **Error Analytics**: Track error frequency and types
4. **Custom Error Pages**: Create specific pages for 404, 403, etc.
5. **Retry Strategies**: Implement exponential backoff for retries
6. **Error Recovery**: Add automatic recovery mechanisms

## Troubleshooting

### Toast Notifications Not Showing
- Ensure `ToastProvider` wraps your app in `App.tsx`
- Check that `useToast` is called within a component inside `ToastProvider`

### Error Boundaries Not Catching Errors
- Error boundaries only catch errors in child components
- They don't catch errors in event handlers (use try-catch)
- They don't catch errors in async code (handle in promises)

### Loading Spinners Not Appearing
- Verify `isLoading` state from React Query hooks
- Check that loading spinner is rendered before data checks

### API Errors Not Formatted Correctly
- Ensure backend returns consistent error format
- Check API client interceptor configuration
- Verify `ApiError` class is being used correctly
