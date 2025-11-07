import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Snackbar,
} from '@mui/material';
import { useState } from 'react';
import { DepartmentForm } from '../../components/forms/DepartmentForm';
import { useCreateDepartment } from '../../hooks/useDepartments';
import type { DepartmentCreate as DepartmentCreateType } from '../../types/department';

export const DepartmentCreate = () => {
  const navigate = useNavigate();
  const [showSuccess, setShowSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const createDepartment = useCreateDepartment({
    onSuccess: () => {
      setShowSuccess(true);
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/departments');
      }, 1500);
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to create department');
    },
  });

  const handleSubmit = (data: DepartmentCreateType) => {
    setErrorMessage(null);
    createDepartment.mutate(data);
  };

  const handleCancel = () => {
    navigate('/departments');
  };

  const handleCloseError = () => {
    setErrorMessage(null);
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Create New Department
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={handleCloseError}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <DepartmentForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={createDepartment.isPending}
          submitLabel="Create Department"
        />
      </Paper>

      <Snackbar
        open={showSuccess}
        autoHideDuration={3000}
        onClose={() => setShowSuccess(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="success" sx={{ width: '100%' }}>
          Department created successfully!
        </Alert>
      </Snackbar>
    </Box>
  );
};
