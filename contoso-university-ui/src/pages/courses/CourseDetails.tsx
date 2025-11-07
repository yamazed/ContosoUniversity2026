import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Button,
  Card,
  CardMedia,
  Divider,
} from '@mui/material';
import { ArrowBack, Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { useCourse } from '../../hooks/useCourses';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const CourseDetails = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const courseId = parseInt(id || '0', 10);

  // Fetch course data
  const { data: course, isLoading, error } = useCourse(courseId);

  const handleBack = () => {
    navigate('/courses');
  };

  const handleEdit = () => {
    navigate(`/courses/${courseId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/courses/${courseId}/delete`);
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading course details..." />;
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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Course Details</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            onClick={handleBack}
          >
            Back to List
          </Button>
          <Button
            variant="contained"
            startIcon={<EditIcon />}
            onClick={handleEdit}
          >
            Edit
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<DeleteIcon />}
            onClick={handleDelete}
          >
            Delete
          </Button>
        </Box>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
          <Box sx={{ flex: 1 }}>
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
          </Box>

          <Box sx={{ flex: 1 }}>
            <Typography variant="h6" gutterBottom>
              Teaching Material
            </Typography>
            <Divider sx={{ mb: 2 }} />

            {course.teachingMaterialImagePath ? (
              <Card>
                <CardMedia
                  component="img"
                  image={course.teachingMaterialImagePath}
                  alt={`Teaching material for ${course.title}`}
                  sx={{ maxHeight: 400, objectFit: 'contain' }}
                />
              </Card>
            ) : (
              <Alert severity="info">No teaching material image available</Alert>
            )}
          </Box>
        </Box>
      </Paper>
    </Box>
  );
};
