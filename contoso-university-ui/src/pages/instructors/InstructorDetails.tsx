import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Card,
  CardContent,
  Grid,
  Button,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Divider,
  Chip,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  LocationOn as LocationIcon,
} from '@mui/icons-material';
import { useInstructor } from '../../hooks/useInstructors';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const InstructorDetails = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const instructorId = parseInt(id || '0', 10);

  // Fetch instructor data
  const { data: instructor, isLoading, error } = useInstructor(instructorId);

  const handleBack = () => {
    navigate('/instructors');
  };

  const handleEdit = () => {
    navigate(`/instructors/${instructorId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/instructors/${instructorId}/delete`);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading instructor details..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading instructor: {error.message}
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
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">
          Instructor Details
        </Typography>
        <Stack direction="row" spacing={2}>
          <Button
            variant="outlined"
            startIcon={<BackIcon />}
            onClick={handleBack}
          >
            Back
          </Button>
          <Button
            variant="outlined"
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
        </Stack>
      </Stack>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Instructor Information
          </Typography>
          <Divider sx={{ mb: 2 }} />
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
                Full Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {instructor.fullName}
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
          </Grid>
        </CardContent>
      </Card>

      {instructor.officeAssignment && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} mb={2}>
              <LocationIcon color="primary" />
              <Typography variant="h6">
                Office Assignment
              </Typography>
            </Stack>
            <Divider sx={{ mb: 2 }} />
            <Typography variant="body1">
              {instructor.officeAssignment.location}
            </Typography>
          </CardContent>
        </Card>
      )}

      {!instructor.officeAssignment && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} mb={2}>
              <LocationIcon color="disabled" />
              <Typography variant="h6" color="text.secondary">
                Office Assignment
              </Typography>
            </Stack>
            <Divider sx={{ mb: 2 }} />
            <Typography variant="body2" color="text.secondary">
              No office assigned
            </Typography>
          </CardContent>
        </Card>
      )}

      {instructor.courseAssignments && instructor.courseAssignments.length > 0 && (
        <Paper>
          <Box sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Assigned Courses
            </Typography>
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Course ID</TableCell>
                  <TableCell>Course Title</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {instructor.courseAssignments.map((assignment) => (
                  <TableRow key={assignment.courseID}>
                    <TableCell>
                      <Chip label={assignment.courseID} size="small" />
                    </TableCell>
                    <TableCell>
                      {assignment.courseTitle || 'No title available'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {(!instructor.courseAssignments || instructor.courseAssignments.length === 0) && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="body2" color="text.secondary" align="center">
            No courses assigned to this instructor.
          </Typography>
        </Paper>
      )}
    </Box>
  );
};
