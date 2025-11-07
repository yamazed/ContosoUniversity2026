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
  Alert,
  Toolbar,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useDepartments } from '../../hooks/useDepartments';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

export const DepartmentList = () => {
  const navigate = useNavigate();

  // Fetch all departments
  const { data: departments, isLoading, error } = useDepartments();

  // Navigation handlers
  const handleView = useCallback((id: number) => {
    navigate(`/departments/${id}`);
  }, [navigate]);

  const handleEdit = useCallback((id: number) => {
    navigate(`/departments/${id}/edit`);
  }, [navigate]);

  const handleDelete = useCallback((id: number) => {
    navigate(`/departments/${id}/delete`);
  }, [navigate]);

  const handleCreate = useCallback(() => {
    navigate('/departments/create');
  }, [navigate]);

  // Format currency for display
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  // Format date for display
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading departments..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Error loading departments: {error.message}
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Departments
      </Typography>

      <Paper sx={{ mb: 2 }}>
        <Toolbar sx={{ justifyContent: 'flex-end' }}>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreate}
          >
            Create New Department
          </Button>
        </Toolbar>
      </Paper>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Administrator</TableCell>
              <TableCell align="right">Budget</TableCell>
              <TableCell>Start Date</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {departments && departments.length > 0 ? (
              departments.map((department) => (
                <TableRow key={department.departmentID} hover>
                  <TableCell>{department.name}</TableCell>
                  <TableCell>
                    {department.administratorName || 'No Administrator'}
                  </TableCell>
                  <TableCell align="right">
                    {formatCurrency(department.budget)}
                  </TableCell>
                  <TableCell>{formatDate(department.startDate)}</TableCell>
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleView(department.departmentID)}
                      aria-label={`View ${department.name}`}
                      title="View"
                    >
                      <ViewIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleEdit(department.departmentID)}
                      aria-label={`Edit ${department.name}`}
                      title="Edit"
                    >
                      <EditIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleDelete(department.departmentID)}
                      aria-label={`Delete ${department.name}`}
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
                    No departments found.
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
