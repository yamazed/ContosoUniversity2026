import { useRouteError, isRouteErrorResponse, Link } from 'react-router-dom';
import { Box, Container, Typography, Button, Paper } from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import HomeIcon from '@mui/icons-material/Home';

export const ErrorPage = () => {
  const error = useRouteError();
  
  let errorMessage = 'An unexpected error occurred';
  let errorStatus = 'Error';
  
  if (isRouteErrorResponse(error)) {
    errorStatus = `${error.status}`;
    errorMessage = error.statusText || errorMessage;
    
    if (error.status === 404) {
      errorMessage = 'Page not found';
    }
  } else if (error instanceof Error) {
    errorMessage = error.message;
  }

  return (
    <Container maxWidth="md">
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          py: 4,
        }}
      >
        <Paper
          elevation={3}
          sx={{
            p: 4,
            textAlign: 'center',
            width: '100%',
          }}
        >
          <ErrorOutlineIcon
            sx={{
              fontSize: 80,
              color: 'error.main',
              mb: 2,
            }}
          />
          <Typography variant="h3" component="h1" gutterBottom>
            {errorStatus}
          </Typography>
          <Typography variant="h5" color="text.secondary" gutterBottom>
            {errorMessage}
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mt: 2, mb: 4 }}>
            Sorry, we couldn't find the page you're looking for.
          </Typography>
          <Button
            component={Link}
            to="/"
            variant="contained"
            startIcon={<HomeIcon />}
            size="large"
          >
            Go to Home
          </Button>
        </Paper>
      </Box>
    </Container>
  );
};
