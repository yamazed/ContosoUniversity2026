import axios, { AxiosError } from 'axios';

// Custom error class for API errors
export class ApiError extends Error {
  public statusCode?: number;
  public errors?: string[];
  
  constructor(
    message: string,
    statusCode?: number,
    errors?: string[]
  ) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.errors = errors;
  }
}

// Create Axios instance with base URL from environment variables
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: false, // Disabled for now - enable when authentication is implemented
  timeout: 30000, // 30 second timeout
});

// Request interceptor to add authentication token
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available (for future token-based auth)
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle errors and provide user-friendly messages
apiClient.interceptors.response.use(
  (response) => {
    // Return the response data directly for successful requests
    return response;
  },
  (error: AxiosError) => {
    let errorMessage = 'An unexpected error occurred';
    let statusCode: number | undefined;
    let errors: string[] | undefined;

    if (error.response) {
      // Server responded with error status
      statusCode = error.response.status;
      const data = error.response.data as any;

      switch (statusCode) {
        case 400:
          errorMessage = 'Invalid request. Please check your input.';
          if (data?.errors) {
            errors = Array.isArray(data.errors) ? data.errors : [data.errors];
          } else if (data?.message) {
            errorMessage = data.message;
          }
          break;
        case 401:
          errorMessage = 'You are not authenticated. Please log in.';
          console.error('Unauthorized access - redirecting to login');
          // Redirect to login page or authentication endpoint
          setTimeout(() => {
            window.location.href = '/login';
          }, 1000);
          break;
        case 403:
          errorMessage = 'You do not have permission to perform this action.';
          break;
        case 404:
          errorMessage = 'The requested resource was not found.';
          break;
        case 409:
          errorMessage = 'A conflict occurred. The data may have been modified by another user.';
          if (data?.message) {
            errorMessage = data.message;
          }
          break;
        case 500:
          errorMessage = 'A server error occurred. Please try again later.';
          break;
        default:
          errorMessage = data?.message || `Server error: ${statusCode}`;
      }
    } else if (error.request) {
      // Request was made but no response received
      if (error.code === 'ECONNABORTED') {
        errorMessage = 'Request timeout. Please check your connection and try again.';
      } else {
        errorMessage = 'Unable to reach the server. Please check your internet connection.';
      }
    } else {
      // Something else happened
      errorMessage = error.message || 'An unexpected error occurred';
    }

    console.error('API Error:', {
      message: errorMessage,
      statusCode,
      errors,
      originalError: error,
    });

    return Promise.reject(new ApiError(errorMessage, statusCode, errors));
  }
);

export default apiClient;
