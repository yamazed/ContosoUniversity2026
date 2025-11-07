import { useState, useMemo, useCallback } from 'react';
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
  TableSortLabel,
  TextField,
  Button,
  IconButton,
  Typography,
  InputAdornment,
  Toolbar,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Search as SearchIcon,
} from '@mui/icons-material';
import { useStudents } from '../../hooks/useStudents';
import { LoadingSpinner, PaginationComponent, ErrorDisplay } from '../../components/common';
import { useDebounce } from '../../hooks/useDebounce';

type SortOrder = 'name_asc' | 'name_desc' | 'date_asc' | 'date_desc' | '';

export const StudentList = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [searchString, setSearchString] = useState('');
  const [sortOrder, setSortOrder] = useState<SortOrder>('');

  // Debounce search input to avoid excessive API calls
  const debouncedSearch = useDebounce(searchString, 500);

  // Fetch students with current parameters
  const { data, isLoading, error } = useStudents({
    page,
    pageSize: 10,
    sortOrder: sortOrder || undefined,
    searchString: debouncedSearch || undefined,
  });

  // Handle page change
  const handlePageChange = useCallback((value: number) => {
    setPage(value);
  }, []);

  // Handle search input change
  const handleSearchChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchString(event.target.value);
    setPage(1); // Reset to first page on search
  }, []);

  // Handle sort change
  const handleSortChange = useCallback((column: 'name' | 'date') => {
    setSortOrder((prevOrder) => {
      if (column === 'name') {
        if (prevOrder === 'name_asc') return 'name_desc';
        return 'name_asc';
      } else {
        if (prevOrder === 'date_asc') return 'date_desc';
        return 'date_asc';
      }
    });
    setPage(1); // Reset to first page on sort
  }, []);

  // Determine sort direction for TableSortLabel
  const getSortDirection = useMemo(() => {
    return (column: 'name' | 'date'): 'asc' | 'desc' | undefined => {
      if (column === 'name') {
        if (sortOrder === 'name_asc') return 'asc';
        if (sortOrder === 'name_desc') return 'desc';
      } else {
        if (sortOrder === 'date_asc') return 'asc';
        if (sortOrder === 'date_desc') return 'desc';
      }
      return undefined;
    };
  }, [sortOrder]);

  // Navigation handlers
  const handleView = useCallback((id: number) => {
    navigate(`/students/${id}`);
  }, [navigate]);

  const handleEdit = useCallback((id: number) => {
    navigate(`/students/${id}/edit`);
  }, [navigate]);

  const handleDelete = useCallback((id: number) => {
    navigate(`/students/${id}/delete`);
  }, [navigate]);

  const handleCreate = useCallback(() => {
    navigate('/students/create');
  }, [navigate]);

  // Format date for display
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading students..." />;
  }

  if (error) {
    return <ErrorDisplay error={error} title="Failed to Load Students" />;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Students
      </Typography>

      <Paper sx={{ mb: 2 }}>
        <Toolbar sx={{ gap: 2, flexWrap: 'wrap' }}>
          <TextField
            placeholder="Search by name..."
            value={searchString}
            onChange={handleSearchChange}
            size="small"
            sx={{ flexGrow: 1, minWidth: 200 }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
          />
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreate}
          >
            Create New Student
          </Button>
        </Toolbar>
      </Paper>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>
                <TableSortLabel
                  active={sortOrder.startsWith('name')}
                  direction={getSortDirection('name')}
                  onClick={() => handleSortChange('name')}
                >
                  Name
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortOrder.startsWith('date')}
                  direction={getSortDirection('date')}
                  onClick={() => handleSortChange('date')}
                >
                  Enrollment Date
                </TableSortLabel>
              </TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items && data.items.length > 0 ? (
              data.items.map((student) => (
                <TableRow key={student.id} hover>
                  <TableCell>
                    {student.lastName}, {student.firstMidName}
                  </TableCell>
                  <TableCell>{formatDate(student.enrollmentDate)}</TableCell>
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleView(student.id)}
                      aria-label={`View ${student.lastName}`}
                      title="View"
                    >
                      <ViewIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => handleEdit(student.id)}
                      aria-label={`Edit ${student.lastName}`}
                      title="Edit"
                    >
                      <EditIcon />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleDelete(student.id)}
                      aria-label={`Delete ${student.lastName}`}
                      title="Delete"
                    >
                      <DeleteIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={3} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 3 }}>
                    {searchString ? 'No students found matching your search.' : 'No students found.'}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {data && (
        <PaginationComponent
          currentPage={page}
          totalPages={data.totalPages}
          onPageChange={handlePageChange}
          hasNext={data.hasNextPage}
          hasPrevious={data.hasPreviousPage}
        />
      )}
    </Box>
  );
};
