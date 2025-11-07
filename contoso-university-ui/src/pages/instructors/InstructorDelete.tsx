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
import { useInstructor, useDeleteInstructor } from '../../hooks/useInstructors';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

export const InstructorDelete = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const instructorId = parseInt(id || '0', 10);

  const [showConfirmDialog, setShowConfirmDialog] = useState(true);
  const [showSuccess, setShowSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Fetch instructor data
  const { data: instructor, isLoading, error: fetchError } = useInstructor(instructorId);

  // Delete mutation
  const deleteInstructor = useDeleteInstructor({
    onSuccess: () => {
      setShowSuccess(true);
      setShowConfirmDialog(false);
      // Navigate to list after a short delay to show success message
      setTimeout(() => {
        navigate('/instructors');
      }, 1500);
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to delete instructor');
      setShowConfirmDialog(false);
    },
  });

  const handleConfirmDelete = () => {
    setErrorMessage(null);
    deleteInstructor.mutate(instructorId);
  };

  const handleCancelDelete = () => {
    setShowConfirmDialog(false);
    navigate('/instructors');
  };

  const handleBack = () => {
    navigate('/instructors');
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading instructor..." />;
  }

  if (fetchError) {
    return (
      <Box>
        <Alert severity="error">
          Error loading instructor: {fetchError.message}
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

  if (!instructor) {
    return (
      <Box>
        <Alert severity="error">
          Instructor not found
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
        Delete Instructor
      </Typography>

      <Alert severity="warning" sx={{ mb: 3 }}>
        Are you sure you want to delete this instructor? This action cannot be undone.
        {instructor.courseAssignments && instructor.courseAssignments.length > 0 && (
          <Typography variant="body2" sx={{ mt: 1 }}>
            Note: This instructor is currently assigned to {instructor.courseAssignments.length} course(s).
          </Typography>
        )}
      </Alert>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Instructor Information
          </Typography>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Last Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {instructor.lastName}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                First Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {instructor.firstMidName}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Hire Date
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatDate(instructor.hireDate)}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Office Location
              </Typography>
              <Typography variant="body1" gutterBottom>
                {instructor.officeAssignment?.location || 'N/A'}
              </Typography>
            </Grid>
            {instructor.courseAssignments && instructor.courseAssignments.length > 0 && (
              <Grid size={{ xs: 12 }}>
                <Typography variant="body2" color="text.secondary">
                  Assigned Courses
                </Typography>
                <Typography variant="body1" gutterBottom>
                  {instructor.courseAssignments.map(ca => ca.courseTitle || `Course ${ca.courseID}`).join(', ')}
                </Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={2}>
        <Button
          variant="outlined"
          startIcon={<BackIcon />}
          onClick={handleBack}
          disabled={deleteInstructor.isPending}
        >
          Back to List
        </Button>
      </Stack>

      <ConfirmDialog
        open={showConfirmDialog}
        title="Confirm Delete"
        message={`Are you sure you want to delete ${instructor.firstMidName} ${instructor.lastName}? This action cannot be undone.`}
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
          Instructor deleted successfully!
        </Alert>
      </Snackbar>
    </Box>
  );
};
