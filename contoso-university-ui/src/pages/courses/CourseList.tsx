import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  IconButton,
  Typography,
  Toolbar,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useCourses } from '../../hooks/useCourses';
import { LoadingSpinner, ErrorDisplay } from '../../components/common';

export const CourseList = () => {
  const navigate = useNavigate();

  // Fetch courses
  const { data: courses, isLoading, error } = useCourses();

  // Navigation handlers
  const handleView = useCallback((id: number) => {
    navigate(`/courses/${id}`);
  }, [navigate]);

  const handleEdit = useCallback((id: number) => {
    navigate(`/courses/${id}/edit`);
  }, [navigate]);

  const handleDelete = useCallback((id: number) => {
    navigate(`/courses/${id}/delete`);
  }, [navigate]);

  const handleCreate = useCallback(() => {
    navigate('/courses/create');
  }, [navigate]);

  if (isLoading) {
    return <LoadingSpinner message="Loading courses..." />;
  }

  if (error) {
    return <ErrorDisplay error={error} title="Failed to Load Courses" />;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Courses
      </Typography>

      <Paper sx={{ mb: 2 }}>
        <Toolbar sx={{ justifyContent: 'flex-end' }}>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreate}
          >
            Create New Course
          </Button>
        </Toolbar>
      </Paper>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Course ID</TableCell>
              <TableCell>Title</TableCell>
              <TableCell>Credits</TableCell>
              <TableCell>Department</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {courses && courses.length > 0 ? (
              courses.map((course) => (
                <TableRow key={course.courseID} hover>
                  <TableCell>{course.courseID}</TableCell>
                  <TableCell>{course.title}</TableCell>
                  <TableCell>{course.credits}</TableCell>
                  <TableCell>{course.departmentName || 'N/A'}</TableCell>
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleView(course.courseID)}
                      aria-label={`View ${course.title}`}
                      title="View"
                    >
                      <ViewIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleEdit(course.courseID)}
                      aria-label={`Edit ${course.title}`}
                      title="Edit"
                    >
                      <EditIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleDelete(course.courseID)}
                      aria-label={`Delete ${course.title}`}
                      title="Delete"
                    >
                      <DeleteIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 3 }}>
                    No courses found.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};
