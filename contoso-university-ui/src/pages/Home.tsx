import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActionArea,
  Container,
} from '@mui/material';
import SchoolIcon from '@mui/icons-material/School';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import PersonIcon from '@mui/icons-material/Person';
import BusinessIcon from '@mui/icons-material/Business';
import NotificationsIcon from '@mui/icons-material/Notifications';
import InfoIcon from '@mui/icons-material/Info';

interface NavigationCard {
  title: string;
  description: string;
  path: string;
  icon: React.ReactElement;
  color: string;
}

const navigationCards: NavigationCard[] = [
  {
    title: 'Students',
    description: 'Manage student records, enrollments, and academic information',
    path: '/students',
    icon: <SchoolIcon sx={{ fontSize: 48 }} />,
    color: '#1976d2',
  },
  {
    title: 'Courses',
    description: 'View and manage course offerings and teaching materials',
    path: '/courses',
    icon: <MenuBookIcon sx={{ fontSize: 48 }} />,
    color: '#2e7d32',
  },
  {
    title: 'Instructors',
    description: 'Manage instructor information, course assignments, and office locations',
    path: '/instructors',
    icon: <PersonIcon sx={{ fontSize: 48 }} />,
    color: '#ed6c02',
  },
  {
    title: 'Departments',
    description: 'Oversee department budgets, administrators, and organizational structure',
    path: '/departments',
    icon: <BusinessIcon sx={{ fontSize: 48 }} />,
    color: '#9c27b0',
  },
  {
    title: 'Notifications',
    description: 'View system notifications and track entity changes',
    path: '/notifications',
    icon: <NotificationsIcon sx={{ fontSize: 48 }} />,
    color: '#d32f2f',
  },
  {
    title: 'About',
    description: 'View enrollment statistics and application information',
    path: '/about',
    icon: <InfoIcon sx={{ fontSize: 48 }} />,
    color: '#0288d1',
  },
];

export const Home = () => {
  const navigate = useNavigate();

  return (
    <Container maxWidth="lg">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom>
          Welcome to Contoso University
        </Typography>
        <Typography variant="h6" color="text.secondary" paragraph>
          A modern platform for managing university operations
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Select a section below to get started
        </Typography>
      </Box>

      <Grid container spacing={3}>
        {navigationCards.map((card) => (
          <Grid size={{ xs: 12, sm: 6, md: 4 }} key={card.path}>
            <Card
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                transition: 'transform 0.2s, box-shadow 0.2s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 6,
                },
              }}
            >
              <CardActionArea
                onClick={() => navigate(card.path)}
                sx={{
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'flex-start',
                  p: 0,
                }}
              >
                <CardContent sx={{ width: '100%', flexGrow: 1 }}>
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      mb: 2,
                      color: card.color,
                    }}
                  >
                    {card.icon}
                  </Box>
                  <Typography variant="h5" component="h2" gutterBottom>
                    {card.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {card.description}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Container>
  );
};
