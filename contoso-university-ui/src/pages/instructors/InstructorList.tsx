import { useState, useCallback, Fragment } from 'react';
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
  Alert,
  Toolbar,
  Grid,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Divider,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useInstructors } from '../../hooks/useInstructors';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import type { Enrollment } from '../../types/student';

export const InstructorList = () => {
  const navigate = useNavigate();
  const [selectedInstructorId, setSelectedInstructorId] = useState<number | null>(null);
  const [selectedCourseId, setSelectedCourseId] = useState<number | null>(null);

  // Fetch all instructors
  const { data: instructors, isLoading, error } = useInstructors({});

  // Navigation handlers
  const handleView = useCallback((id: number) => {
    navigate(`/instructors/${id}`);
  }, [navigate]);

  const handleEdit = useCallback((id: number) => {
    navigate(`/instructors/${id}/edit`);
  }, [navigate]);

  const handleDelete = useCallback((id: number) => {
    navigate(`/instructors/${id}/delete`);
  }, [navigate]);

  const handleCreate = useCallback(() => {
    navigate('/instructors/create');
  }, [navigate]);

  // Selection handlers
  const handleInstructorSelect = useCallback((instructorId: number) => {
    setSelectedInstructorId(instructorId);
    setSelectedCourseId(null); // Reset course selection
  }, []);

  const handleCourseSelect = useCallback((courseId: number) => {
    setSelectedCourseId(courseId);
  }, []);

  // Get selected instructor
  const selectedInstructor = instructors?.find(i => i.id === selectedInstructorId);

  // Get enrollments from the selected course
  const getEnrollmentsForCourse = (): Enrollment[] => {
    // In a real implementation, this would come from the API
    // For now, we'll return an empty array as the API structure needs to support this
    return [];
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading instructors..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading instructors: {error.message}
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Instructors
      </Typography>

      <Paper sx={{ mb: 2 }}>
        <Toolbar>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreate}
          >
            Create New Instructor
          </Button>
        </Toolbar>
      </Paper>

      <Grid container spacing={2}>
        {/* Instructors List */}
        <Grid size={{ xs: 12, md: 4 }}>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Office</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {instructors && instructors.length > 0 ? (
                  instructors.map((instructor) => (
                    <TableRow
                      key={instructor.id}
                      hover
                      selected={selectedInstructorId === instructor.id}
                      onClick={() => handleInstructorSelect(instructor.id)}
                      sx={{ cursor: 'pointer' }}
                    >
                      <TableCell>
                        {instructor.lastName}, {instructor.firstMidName}
                      </TableCell>
                      <TableCell>
                        {instructor.officeAssignment?.location || 'N/A'}
                      </TableCell>
                      <TableCell align="right">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleView(instructor.id);
                          }}
                          aria-label={`View ${instructor.lastName}`}
                          title="View"
                        >
                          <ViewIcon fontSize="small" />
                        </IconButton>
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleEdit(instructor.id);
                          }}
                          aria-label={`Edit ${instructor.lastName}`}
                          title="Edit"
                        >
                          <EditIcon fontSize="small" />
                        </IconButton>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDelete(instructor.id);
                          }}
                          aria-label={`Delete ${instructor.lastName}`}
                          title="Delete"
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={3} align="center">
                      <Typography variant="body2" color="text.secondary" sx={{ py: 3 }}>
                        No instructors found.
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Grid>

        {/* Courses for Selected Instructor */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Courses
              </Typography>
              {selectedInstructor ? (
                <>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Courses taught by {selectedInstructor.fullName}
                  </Typography>
                  {selectedInstructor.courseAssignments && selectedInstructor.courseAssignments.length > 0 ? (
                    <List dense>
                      {selectedInstructor.courseAssignments.map((assignment) => (
                        <ListItem key={assignment.courseID} disablePadding>
                          <ListItemButton
                            selected={selectedCourseId === assignment.courseID}
                            onClick={() => handleCourseSelect(assignment.courseID)}
                          >
                            <ListItemText
                              primary={assignment.courseTitle || `Course ${assignment.courseID}`}
                              secondary={`Course ID: ${assignment.courseID}`}
                            />
                          </ListItemButton>
                        </ListItem>
                      ))}
                    </List>
                  ) : (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                      No courses assigned.
                    </Typography>
                  )}
                </>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Select an instructor to view their courses.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Enrollments for Selected Course */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Students
              </Typography>
              {selectedCourseId ? (
                <>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Students enrolled in selected course
                  </Typography>
                  {getEnrollmentsForCourse().length > 0 ? (
                    <List dense>
                      {getEnrollmentsForCourse().map((enrollment) => (
                        <Fragment key={enrollment.enrollmentID}>
                          <ListItem>
                            <ListItemText
                              primary={`Student ID: ${enrollment.studentID}`}
                              secondary={enrollment.grade ? `Grade: ${enrollment.grade}` : 'No grade'}
                            />
                          </ListItem>
                          <Divider />
                        </Fragment>
                      ))}
                    </List>
                  ) : (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                      No students enrolled in this course.
                    </Typography>
                  )}
                </>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Select a course to view enrolled students.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};
