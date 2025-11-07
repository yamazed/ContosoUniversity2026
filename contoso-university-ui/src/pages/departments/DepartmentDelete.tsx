import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Alert,
  Card,
  CardContent,
  Grid,
  Button,
  Stack,
  Snackbar,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useDepartment, useDeleteDepartment } from '../../hooks/useDepartments';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

export const DepartmentDelete = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const departmentId = parseInt(id || '0', 10);

  const [showConfirmDialog, setShowConfirmDialog] = useState(true);
  const [showSuccess, setShowSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Fetch department data
  const { data: department, isLoading, error: fetchError } = useDepartment(departmentId);

  // Delete mutation
  const deleteDepartment = useDeleteDepartment({
    onSuccess: () => {
      setShowSuccess(true);
      setShowConfirmDialog(false);
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/departments');
      }, 1500);
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to delete department');
      setShowConfirmDialog(false);
    },
  });

  const handleConfirmDelete = () => {
    setErrorMessage(null);
    deleteDepartment.mutate(departmentId);
  };

  const handleCancelDelete = () => {
    setShowConfirmDialog(false);
    navigate('/departments');
  };

  const handleBack = () => {
    navigate('/departments');
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading department..." />;
  }

  if (fetchError) {
    return (
      <Box>
        <Alert severity="error">
          Error loading department: {fetchError.message}
        </Alert>
        <Button
          startIcon={<BackIcon />}
          onClick={handleBack}
          sx={{ mt: 2 }}
        >
          Back to List
        </Button>
      </Box>
    );
  }

  if (!department) {
    return (
      <Box>
        <Alert severity="error">
          Department not found
        </Alert>
        <Button
          startIcon={<BackIcon />}
          onClick={handleBack}
          sx={{ mt: 2 }}
        >
          Back to List
        </Button>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Delete Department
      </Typography>

      <Alert severity="warning" sx={{ mb: 3 }}>
        Are you sure you want to delete this department? This action cannot be undone.
      </Alert>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Department Information
          </Typography>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Department Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {department.name}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Administrator
              </Typography>
              <Typography variant="body1" gutterBottom>
                {department.administratorName || 'No Administrator'}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Budget
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatCurrency(department.budget)}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Start Date
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatDate(department.startDate)}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={2}>
        <Button
          variant="outlined"
          startIcon={<BackIcon />}
          onClick={handleBack}
          disabled={deleteDepartment.isPending}
        >
          Back to List
        </Button>
      </Stack>

      <ConfirmDialog
        open={showConfirmDialog}
        title="Confirm Delete"
        message={`Are you sure you want to delete the ${department.name} department? This action cannot be undone.`}
        confirmText="Delete"
        cancelText="Cancel"
        confirmColor="error"
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
      />

      <Snackbar
        open={showSuccess}
        autoHideDuration={3000}
        onClose={() => setShowSuccess(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="success" sx={{ width: '100%' }}>
          Department deleted successfully!
        </Alert>
      </Snackbar>
    </Box>
  );
};
