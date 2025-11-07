import { Alert, AlertTitle, Box, Button, List, ListItem, ListItemText } from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { ApiError } from '../../api/client';

interface ErrorDisplayProps {
  error: Error | ApiError | unknown;
  onRetry?: () => void;
  title?: string;
}

export const ErrorDisplay = ({ error, onRetry, title = 'Error' }: ErrorDisplayProps) => {
  const getErrorMessage = (err: unknown): string => {
    if (err instanceof ApiError) {
      return err.message;
    }
    if (err instanceof Error) {
      return err.message;
    }
    if (typeof err === 'string') {
      return err;
    }
    return 'An unexpected error occurred';
  };

  const getErrorDetails = (err: unknown): string[] | undefined => {
    if (err instanceof ApiError && err.errors) {
      return err.errors;
    }
    return undefined;
  };

  const message = getErrorMessage(error);
  const details = getErrorDetails(error);

  return (
    <Box sx={{ my: 2 }}>
      <Alert
        severity="error"
        action={
          onRetry && (
            <Button
              color="inherit"
              size="small"
              startIcon={<RefreshIcon />}
              onClick={onRetry}
            >
              Retry
            </Button>
          )
        }
      >
        <AlertTitle>{title}</AlertTitle>
        {message}
        {details && details.length > 0 && (
          <List dense sx={{ mt: 1 }}>
            {details.map((detail, index) => (
              <ListItem key={index} sx={{ py: 0 }}>
                <ListItemText primary={`• ${detail}`} />
              </ListItem>
            ))}
          </List>
        )}
      </Alert>
    </Box>
  );
};
