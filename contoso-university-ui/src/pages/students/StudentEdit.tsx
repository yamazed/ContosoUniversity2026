import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
} from '@mui/material';
import { StudentForm, type StudentFormData } from '../../components/forms/StudentForm';
import { useStudent, useUpdateStudent } from '../../hooks/useStudents';
import { useToast } from '../../contexts/ToastContext';
import { LoadingSpinner, ErrorDisplay } from '../../components/common';
import type { StudentUpdate } from '../../types/student';

export const StudentEdit = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const studentId = parseInt(id || '0', 10);
  const { showSuccess, showError } = useToast();

  // Fetch student data
  const { data: student, isLoading, error: fetchError, refetch } = useStudent(studentId);

  // Update mutation
  const updateStudent = useUpdateStudent({
    onSuccess: () => {
      showSuccess('Student updated successfully!');
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/students');
      }, 1000);
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to update student');
    },
  });

  const handleSubmit = (data: StudentUpdate | { lastName: string; firstMidName: string; enrollmentDate: string }) => {
    updateStudent.mutate({
      id: studentId,
      data: {
        ...data,
        id: studentId,
      },
    });
  };

  const handleCancel = () => {
    navigate('/students');
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading student..." />;
  }

  if (fetchError) {
    return (
      <Box>
        <ErrorDisplay 
          error={fetchError} 
          title="Failed to Load Student"
          onRetry={() => refetch()}
        />
      </Box>
    );
  }

  if (!student) {
    return (
      <Box>
        <ErrorDisplay 
          error="Student not found" 
          title="Not Found"
        />
      </Box>
    );
  }

  // Prepare initial data for form
  const initialData: Partial<StudentFormData> = {
    lastName: student.lastName,
    firstMidName: student.firstMidName,
    enrollmentDate: new Date(student.enrollmentDate),
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Edit Student
      </Typography>

      {updateStudent.isError && (
        <ErrorDisplay 
          error={updateStudent.error} 
          title="Failed to Update Student"
          onRetry={() => updateStudent.reset()}
        />
      )}

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <StudentForm
          initialData={initialData}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={updateStudent.isPending}
          submitLabel="Update Student"
        />
      </Paper>
    </Box>
  );
};
