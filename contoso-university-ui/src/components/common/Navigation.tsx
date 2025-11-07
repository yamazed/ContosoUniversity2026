import { Link, useLocation } from 'react-router-dom';
import type { ReactElement } from 'react';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  IconButton,
  Box,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import SchoolIcon from '@mui/icons-material/School';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import PersonIcon from '@mui/icons-material/Person';
import BusinessIcon from '@mui/icons-material/Business';
import NotificationsIcon from '@mui/icons-material/Notifications';
import InfoIcon from '@mui/icons-material/Info';
import CloseIcon from '@mui/icons-material/Close';

interface NavigationProps {
  open: boolean;
  onClose: () => void;
}

interface NavItem {
  text: string;
  path: string;
  icon: ReactElement;
}

const navItems: NavItem[] = [
  { text: 'Home', path: '/', icon: <HomeIcon /> },
  { text: 'Students', path: '/students', icon: <SchoolIcon /> },
  { text: 'Courses', path: '/courses', icon: <MenuBookIcon /> },
  { text: 'Instructors', path: '/instructors', icon: <PersonIcon /> },
  { text: 'Departments', path: '/departments', icon: <BusinessIcon /> },
  { text: 'Notifications', path: '/notifications', icon: <NotificationsIcon /> },
  { text: 'About', path: '/about', icon: <InfoIcon /> },
];

export const Navigation = ({ open, onClose }: NavigationProps) => {
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const drawerWidth = 240;

  const isActive = (path: string): boolean => {
    if (path === '/') {
      return location.pathname === '/';
    }
    return location.pathname.startsWith(path);
  };

  const drawer = (
    <Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          p: 2,
        }}
      >
        <Box component="span" sx={{ fontWeight: 'bold', fontSize: '1.2rem' }}>
          Navigation
        </Box>
        {isMobile && (
          <IconButton onClick={onClose} edge="end">
            <CloseIcon />
          </IconButton>
        )}
      </Box>
      <Divider />
      <List>
        {navItems.map((item) => (
          <ListItem key={item.path} disablePadding>
            <ListItemButton
              component={Link}
              to={item.path}
              selected={isActive(item.path)}
              onClick={isMobile ? onClose : undefined}
              sx={{
                '&.Mui-selected': {
                  backgroundColor: theme.palette.primary.light,
                  color: theme.palette.primary.contrastText,
                  '&:hover': {
                    backgroundColor: theme.palette.primary.main,
                  },
                  '& .MuiListItemIcon-root': {
                    color: theme.palette.primary.contrastText,
                  },
                },
              }}
            >
              <ListItemIcon
                sx={{
                  color: isActive(item.path)
                    ? theme.palette.primary.contrastText
                    : 'inherit',
                }}
              >
                {item.icon}
              </ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Box>
  );

  return (
    <Drawer
      variant={isMobile ? 'temporary' : 'persistent'}
      open={open}
      onClose={onClose}
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: drawerWidth,
          boxSizing: 'border-box',
        },
      }}
      ModalProps={{
        keepMounted: true, // Better mobile performance
      }}
    >
      {drawer}
    </Drawer>
  );
};
