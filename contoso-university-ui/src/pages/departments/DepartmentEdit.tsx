import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Snackbar,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
} from '@mui/material';
import { useState } from 'react';
import { DepartmentForm, type DepartmentFormData } from '../../components/forms/DepartmentForm';
import { useDepartment, useUpdateDepartment } from '../../hooks/useDepartments';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import type { DepartmentCreate, DepartmentUpdate } from '../../types/department';
import { AxiosError } from 'axios';

interface ConflictData {
  currentValues: {
    name: string;
    budget: number;
    startDate: string;
    administratorName?: string;
  };
  message: string;
}

export const DepartmentEdit = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const departmentId = id ? parseInt(id, 10) : 0;

  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [conflictData, setConflictData] = useState<ConflictData | null>(null);
  const [showConflictDialog, setShowConflictDialog] = useState(false);

  // Fetch department data
  const { data: department, isLoading, error, refetch } = useDepartment(departmentId);

  const updateDepartment = useUpdateDepartment({
    onSuccess: () => {
      setSuccessMessage('Department updated successfully!');
      // Navigate to list after a short delay to show the success message
      setTimeout(() => {
        navigate('/departments');
      }, 1500);
    },
    onError: (error) => {
      // Check if it's a 409 Conflict error (concurrency conflict)
      if (error instanceof AxiosError && error.response?.status === 409) {
        const conflictInfo = error.response.data;
        setConflictData({
          currentValues: conflictInfo.currentValues || {},
          message: conflictInfo.message || 'The department has been modified by another user.',
        });
        setShowConflictDialog(true);
      } else {
        setErrorMessage(error.message || 'Failed to update department');
      }
    },
  });

  const handleSubmit = (data: DepartmentCreate | DepartmentUpdate) => {
    updateDepartment.mutate({
      id: departmentId,
      data: {
        ...data,
        departmentID: departmentId,
      } as DepartmentUpdate,
    });
  };

  const handleCancel = () => {
    navigate('/departments');
  };

  const handleConflictRetry = async () => {
    // Refetch the latest data from the server
    await refetch();
    setShowConflictDialog(false);
    setConflictData(null);
  };

  const handleConflictCancel = () => {
    setShowConflictDialog(false);
    setConflictData(null);
    navigate('/departments');
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading department..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading department: {error.message}
        </Alert>
      </Box>
    );
  }

  if (!department) {
    return (
      <Box>
        <Alert severity="error">
          Department not found
        </Alert>
      </Box>
    );
  }

  // Prepare initial data for the form
  const initialData: Partial<DepartmentFormData> = {
    name: department.name,
    budget: department.budget,
    startDate: new Date(department.startDate),
    instructorID: department.instructorID,
    rowVersion: department.rowVersion,
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Edit Department
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <DepartmentForm
          initialData={initialData}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={updateDepartment.isPending}
          submitLabel="Update Department"
          departmentId={departmentId}
        />
      </Paper>

      <Snackbar
        open={!!successMessage}
        autoHideDuration={3000}
        onClose={() => setSuccessMessage('')}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setSuccessMessage('')}>
          {successMessage}
        </Alert>
      </Snackbar>

      {/* Concurrency Conflict Dialog */}
      <Dialog
        open={showConflictDialog}
        onClose={handleConflictCancel}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Concurrency Conflict</DialogTitle>
        <DialogContent>
          <DialogContentText>
            {conflictData?.message || 'The department has been modified by another user.'}
          </DialogContentText>
          {conflictData?.currentValues && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Current values in the database:
              </Typography>
              <Typography variant="body2">
                <strong>Name:</strong> {conflictData.currentValues.name}
              </Typography>
              <Typography variant="body2">
                <strong>Budget:</strong> ${conflictData.currentValues.budget.toLocaleString()}
              </Typography>
              <Typography variant="body2">
                <strong>Start Date:</strong> {new Date(conflictData.currentValues.startDate).toLocaleDateString()}
              </Typography>
              {conflictData.currentValues.administratorName && (
                <Typography variant="body2">
                  <strong>Administrator:</strong> {conflictData.currentValues.administratorName}
                </Typography>
              )}
            </Box>
          )}
          <Typography variant="body2" sx={{ mt: 2 }}>
            Would you like to reload the current values and try again?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleConflictCancel} color="inherit">
            Cancel
          </Button>
          <Button onClick={handleConflictRetry} variant="contained" autoFocus>
            Reload and Retry
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
