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
  Divider,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useDepartment } from '../../hooks/useDepartments';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const DepartmentDetails = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const departmentId = parseInt(id || '0', 10);

  // Fetch department data
  const { data: department, isLoading, error } = useDepartment(departmentId);

  const handleBack = () => {
    navigate('/departments');
  };

  const handleEdit = () => {
    navigate(`/departments/${departmentId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/departments/${departmentId}/delete`);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading department details..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading department: {error.message}
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

  if (!department) {
    return (
      <Box>
        <Alert severity="error">
          Department not found
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
          Department Details
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

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Department Information
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Department Name
              </Typography>
              <Typography variant="body1" gutterBottom>
                {department.name}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Administrator
              </Typography>
              <Typography variant="body1" gutterBottom>
                {department.administratorName || 'No Administrator'}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Budget
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatCurrency(department.budget)}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography variant="body2" color="text.secondary">
                Start Date
              </Typography>
              <Typography variant="body1" gutterBottom>
                {formatDate(department.startDate)}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
    </Box>
  );
};
