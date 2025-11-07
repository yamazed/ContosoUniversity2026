import {
  Box,
  Typography,
  Card,
  CardContent,
  Container,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Alert,
} from '@mui/material';
import { LoadingSpinner } from '../components/common';
import { useEnrollmentStats } from '../hooks/useEnrollmentStats';

export const About = () => {
  const { data, isLoading, error } = useEnrollmentStats();

  const formatDate = (dateString: string | null): string => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <Container maxWidth="lg">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom>
          About Contoso University
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Contoso University is a modern educational institution management system
          built with cutting-edge technology to streamline academic operations.
        </Typography>
      </Box>

      <Card sx={{ mb: 4 }}>
        <CardContent>
          <Typography variant="h5" component="h2" gutterBottom>
            Student Enrollment Statistics
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            The following table shows the number of students enrolled by date.
          </Typography>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <LoadingSpinner />
            </Box>
          )}

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              Failed to load enrollment statistics. Please try again later.
            </Alert>
          )}

          {data && data.success && data.data && data.data.length > 0 && (
            <TableContainer component={Paper} sx={{ mt: 2 }}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>Enrollment Date</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Number of Students</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.data.map((stat, index) => (
                    <TableRow
                      key={index}
                      sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                    >
                      <TableCell component="th" scope="row">
                        {formatDate(stat.enrollmentDate)}
                      </TableCell>
                      <TableCell align="right">{stat.studentCount}</TableCell>
                    </TableRow>
                  ))}
                  <TableRow sx={{ backgroundColor: 'action.hover' }}>
                    <TableCell>
                      <strong>Total Students</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>
                        {data.data.reduce((sum, stat) => sum + stat.studentCount, 0)}
                      </strong>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {data && data.success && (!data.data || data.data.length === 0) && (
            <Alert severity="info" sx={{ mt: 2 }}>
              No enrollment data available at this time.
            </Alert>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h5" component="h2" gutterBottom>
            Technology Stack
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            This application is built using modern web technologies:
          </Typography>
          <Box component="ul" sx={{ pl: 2 }}>
            <Typography component="li" variant="body2" paragraph>
              <strong>Frontend:</strong> React 18 with TypeScript, Material-UI, React Router, React Query
            </Typography>
            <Typography component="li" variant="body2" paragraph>
              <strong>Backend:</strong> ASP.NET Core 8.0 Web API with Entity Framework Core
            </Typography>
            <Typography component="li" variant="body2" paragraph>
              <strong>Database:</strong> SQL Server
            </Typography>
            <Typography component="li" variant="body2">
              <strong>Architecture:</strong> Single Page Application (SPA) with RESTful API
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
};
