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
} from '@mui/material';
import {
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useStudent, useDeleteStudent } from '../../hooks/useStudents';
import { useToast } from '../../contexts/ToastContext';
import { LoadingSpinner, ErrorDisplay, ConfirmDialog } from '../../components/common';

export const StudentDelete = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const studentId = parseInt(id || '0', 10);
  const { showSuccess, showError } = useToast();

  const [showConfirmDialog, setShowConfirmDialog] = useState(true);

  // Fetch student data
  const { data: student, isLoading, error: fetchError, refetch } = useStudent(studentId);

  // Delete mutation
  const deleteStudent = useDeleteStudent({
    onSuccess: () => {
      showSuccess('Student deleted successfully!');
      setShowConfirmDialog(false);
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/students');
      }, 1000);
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to delete student');
      setShowConfirmDialog(false);
    },
  });

  const handleConfirmDelete = () => {
    deleteStudent.mutate(studentId);
  };

  const handleCancelDelete = () => {
    setShowConfirmDialog(false);
    navigate('/students');
  };

  const handleBack = () => {
    navigate('/students');
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
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

  if (!student) {
    return (
      <Box>
        <ErrorDisplay 
          error="Student not found" 
          title="Not Found"
        />
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
        Delete Student
      </Typography>

      <Alert severity="warning" sx={{ mb: 3 }}>
        Are you sure you want to delete this student? This action cannot be undone.
      </Alert>

      {deleteStudent.isError && (
        <ErrorDisplay 
          error={deleteStudent.error} 
          title="Failed to Delete Student"
          onRetry={() => deleteStudent.reset()}
        />
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Student Information
          </Typography>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Last Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {student.lastName}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                First Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {student.firstMidName}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Enrollment Date
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatDate(student.enrollmentDate)}
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
          disabled={deleteStudent.isPending}
        >
          Back to List
        </Button>
      </Stack>

      <ConfirmDialog
        open={showConfirmDialog}
        title="Confirm Delete"
        message={`Are you sure you want to delete ${student.firstMidName} ${student.lastName}? This action cannot be undone.`}
        confirmText="Delete"
        cancelText="Cancel"
        confirmColor="error"
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
      />
    </Box>
  );
};
