import { useNavigate, useParams } from 'react-router-dom';
import { Box, Paper, Typography, Alert, Snackbar } from '@mui/material';
import { useState } from 'react';
import { CourseForm, type CourseFormData } from '../../components/forms/CourseForm';
import { useCourse, useUpdateCourse } from '../../hooks/useCourses';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const CourseEdit = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const courseId = parseInt(id || '0', 10);

  const [showSuccess, setShowSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string>('');

  // Fetch course data
  const { data: course, isLoading, error } = useCourse(courseId);

  const updateCourseMutation = useUpdateCourse({
    onSuccess: () => {
      setShowSuccess(true);
      setTimeout(() => {
        navigate('/courses');
      }, 1500);
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to update course');
    },
  });

  const handleSubmit = (data: CourseFormData) => {
    setErrorMessage('');
    updateCourseMutation.mutate({
      id: courseId,
      data: {
        courseID: data.courseID,
        title: data.title,
        credits: data.credits,
        departmentID: data.departmentID,
        teachingMaterialImage: data.teachingMaterialImage,
      },
    });
  };

  const handleCancel = () => {
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
        Edit Course
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3, mt: 2 }}>
        <CourseForm
          initialData={{
            courseID: course.courseID,
            title: course.title,
            credits: course.credits,
            departmentID: course.departmentID,
          }}
          existingImagePath={course.teachingMaterialImagePath}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={updateCourseMutation.isPending}
          submitLabel="Update Course"
        />
      </Paper>

      <Snackbar
        open={showSuccess}
        autoHideDuration={6000}
        onClose={() => setShowSuccess(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setShowSuccess(false)}>
          Course updated successfully!
        </Alert>
      </Snackbar>
    </Box>
  );
};
