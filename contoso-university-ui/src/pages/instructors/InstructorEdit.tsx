import { useNavigate, useParams } from 'react-router-dom';
import { Box, Paper, Typography, Alert, Snackbar } from '@mui/material';
import { useState } from 'react';
import { InstructorForm, type InstructorFormData } from '../../components/forms/InstructorForm';
import { useInstructor, useUpdateInstructor } from '../../hooks/useInstructors';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import type { InstructorCreate as InstructorCreateType, InstructorUpdate } from '../../types/instructor';

export const InstructorEdit = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const instructorId = id ? parseInt(id, 10) : 0;

  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Fetch instructor data
  const { data: instructor, isLoading, error } = useInstructor(instructorId);

  const updateInstructor = useUpdateInstructor({
    onSuccess: () => {
      setSuccessMessage('Instructor updated successfully!');
      // Navigate to list after a short delay to show the success message
      setTimeout(() => {
        navigate('/instructors');
      }, 1500);
    },
    onError: (error) => {
      setErrorMessage(error.message || 'Failed to update instructor');
    },
  });

  const handleSubmit = (data: InstructorCreateType | InstructorUpdate) => {
    updateInstructor.mutate({
      id: instructorId,
      data: {
        ...data,
        id: instructorId,
      } as InstructorUpdate,
    });
  };

  const handleCancel = () => {
    navigate('/instructors');
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading instructor..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading instructor: {error.message}
        </Alert>
      </Box>
    );
  }

  if (!instructor) {
    return (
      <Box>
        <Alert severity="error">
          Instructor not found
        </Alert>
      </Box>
    );
  }

  // Prepare initial data for the form
  const initialData: Partial<InstructorFormData> = {
    lastName: instructor.lastName,
    firstMidName: instructor.firstMidName,
    hireDate: new Date(instructor.hireDate),
    officeLocation: instructor.officeAssignment?.location || '',
    courseIDs: instructor.courseAssignments?.map(ca => ca.courseID) || [],
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Edit Instructor
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <InstructorForm
          initialData={initialData}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={updateInstructor.isPending}
          submitLabel="Update Instructor"
          instructorId={instructorId}
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
