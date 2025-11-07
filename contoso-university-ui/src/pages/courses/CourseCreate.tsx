import { useNavigate } from 'react-router-dom';
import { Box, Paper, Typography, Alert, Snackbar } from '@mui/material';
import { useState } from 'react';
import { CourseForm, type CourseFormData } from '../../components/forms/CourseForm';
import { useCreateCourse } from '../../hooks/useCourses';

export const CourseCreate = () => {
  const navigate = useNavigate();
  const [showSuccess, setShowSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string>('');

  const createCourseMutation = useCreateCourse({
    onSuccess: () => {
      setShowSuccess(true);
      setTimeout(() => {
        navigate('/courses');
      }, 1500);
    },
    onError: (error: Error) => {
      setErrorMessage(error.message || 'Failed to create course');
    },
  });

  const handleSubmit = (data: CourseFormData) => {
    setErrorMessage('');
    createCourseMutation.mutate({
      courseID: data.courseID,
      title: data.title,
      credits: data.credits,
      departmentID: data.departmentID,
      teachingMaterialImage: data.teachingMaterialImage,
    });
  };

  const handleCancel = () => {
    navigate('/courses');
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Create New Course
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      <Paper sx={{ p: 3, mt: 2 }}>
        <CourseForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={createCourseMutation.isPending}
          submitLabel="Create Course"
        />
      </Paper>

      <Snackbar
        open={showSuccess}
        autoHideDuration={6000}
        onClose={() => setShowSuccess(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setShowSuccess(false)}>
          Course created successfully!
        </Alert>
      </Snackbar>
    </Box>
  );
};
