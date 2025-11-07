import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Button,
  Divider,
} from '@mui/material';
import { ArrowBack } from '@mui/icons-material';
import { useCourse, useDeleteCourse } from '../../hooks/useCourses';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

export const CourseDelete = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const courseId = parseInt(id || '0', 10);

  const [showConfirm, setShowConfirm] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string>('');

  // Fetch course data
  const { data: course, isLoading, error } = useCourse(courseId);

  const deleteCourseMutation = useDeleteCourse({
    onSuccess: () => {
      navigate('/courses');
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to delete course');
      setShowConfirm(false);
    },
  });

  const handleConfirmDelete = () => {
    setErrorMessage('');
    deleteCourseMutation.mutate(courseId);
  };

  const handleCancelDelete = () => {
    navigate('/courses');
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading course..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading course: {error.message}
        </Alert>
      </Box>
    );
  }

  if (!course) {
    return (
      <Box>
        <Alert severity="error">Course not found</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Delete Course
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3, mt: 2 }}>
        <Alert severity="warning" sx={{ mb: 3 }}>
          Are you sure you want to delete this course? This action cannot be undone.
        </Alert>

        <Typography variant="h6" gutterBottom>
          Course Information
        </Typography>
        <Divider sx={{ mb: 2 }} />

        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Course ID
          </Typography>
          <Typography variant="body1">{course.courseID}</Typography>
        </Box>

        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Title
          </Typography>
          <Typography variant="body1">{course.title}</Typography>
        </Box>

        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Credits
          </Typography>
          <Typography variant="body1">{course.credits}</Typography>
        </Box>

        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Department
          </Typography>
          <Typography variant="body1">{course.departmentName || 'N/A'}</Typography>
        </Box>

        <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
          <Button
            variant="contained"
            color="error"
            onClick={() => setShowConfirm(true)}
            disabled={deleteCourseMutation.isPending}
          >
            {deleteCourseMutation.isPending ? 'Deleting...' : 'Delete Course'}
          </Button>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            onClick={handleCancelDelete}
            disabled={deleteCourseMutation.isPending}
          >
            Cancel
          </Button>
        </Box>
      </Paper>

      <ConfirmDialog
        open={showConfirm}
        title="Confirm Delete"
        message={`Are you sure you want to delete the course "${course.title}"? This action cannot be undone.`}
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
        confirmText="Delete"
        cancelText="Cancel"
        confirmColor="error"
      />
    </Box>
  );
};
