import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
} from '@mui/material';
import { StudentForm } from '../../components/forms/StudentForm';
import { useCreateStudent } from '../../hooks/useStudents';
import { useToast } from '../../contexts/ToastContext';
import { ErrorDisplay } from '../../components/common';
import type { StudentCreate as StudentCreateType } from '../../types/student';

export const StudentCreate = () => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();

  const createStudent = useCreateStudent({
    onSuccess: () => {
      showSuccess('Student created successfully!');
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/students');
      }, 1000);
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to create student');
    },
  });

  const handleSubmit = (data: StudentCreateType) => {
    createStudent.mutate(data);
  };

  const handleCancel = () => {
    navigate('/students');
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Create New Student
      </Typography>

      {createStudent.isError && (
        <ErrorDisplay 
          error={createStudent.error} 
          title="Failed to Create Student"
          onRetry={() => createStudent.reset()}
        />
      )}

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <StudentForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={createStudent.isPending}
          submitLabel="Create Student"
        />
      </Paper>
    </Box>
  );
};
