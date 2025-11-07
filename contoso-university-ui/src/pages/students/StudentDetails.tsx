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
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useStudent } from '../../hooks/useStudents';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const StudentDetails = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const studentId = parseInt(id || '0', 10);

  // Fetch student data
  const { data: student, isLoading, error } = useStudent(studentId);

  const handleBack = () => {
    navigate('/students');
  };

  const handleEdit = () => {
    navigate(`/students/${studentId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/students/${studentId}/delete`);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading student details..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading student: {error.message}
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

  if (!student) {
    return (
      <Box>
        <Alert severity="error">
          Student not found
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
          Student Details
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
            Student Information
          </Typography>
          <Divider sx={{ mb: 2 }} />
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

      {student.enrollments && student.enrollments.length > 0 && (
        <Paper>
          <Box sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Enrollments
            </Typography>
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Course</TableCell>
                  <TableCell>Grade</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {student.enrollments.map((enrollment) => (
                  <TableRow key={enrollment.enrollmentID}>
                    <TableCell>
                      {enrollment.courseTitle || `Course ${enrollment.courseID}`}
                    </TableCell>
                    <TableCell>
                      {enrollment.grade || 'No grade'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {(!student.enrollments || student.enrollments.length === 0) && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="body2" color="text.secondary" align="center">
            No enrollments found for this student.
          </Typography>
        </Paper>
      )}
    </Box>
  );
};
