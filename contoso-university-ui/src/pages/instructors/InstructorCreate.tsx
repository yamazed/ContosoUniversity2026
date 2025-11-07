import { useNavigate } from 'react-router-dom';
import { Box, Paper, Typography, Alert, Snackbar } from '@mui/material';
import { useState } from 'react';
import { InstructorForm } from '../../components/forms/InstructorForm';
import { useCreateInstructor } from '../../hooks/useInstructors';
import type { InstructorCreate as InstructorCreateType } from '../../types/instructor';

export const InstructorCreate = () => {
  const navigate = useNavigate();
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  const createInstructor = useCreateInstructor({
    onSuccess: () => {
      setSuccessMessage('Instructor created successfully!');
      // Navigate to list after a short delay to show the success message
      setTimeout(() => {
        navigate('/instructors');
      }, 1500);
    },
    onError: (error) => {
      setErrorMessage(error.message || 'Failed to create instructor');
    },
  });

  const handleSubmit = (data: InstructorCreateType) => {
    createInstructor.mutate(data);
  };

  const handleCancel = () => {
    navigate('/instructors');
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Create New Instructor
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <InstructorForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={createInstructor.isPending}
          submitLabel="Create Instructor"
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
    </Box>
  );
};
