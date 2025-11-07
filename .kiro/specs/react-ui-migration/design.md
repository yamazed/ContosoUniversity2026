# Design Document

## Overview

This design document outlines the architecture and implementation approach for migrating the Contoso University application from an ASP.NET MVC architecture with Razor Views to a modern Single Page Application using React for the frontend and REST API controllers for the backend. The solution will maintain all existing functionality while providing a more responsive and maintainable architecture with separate frontend and backend deployments.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────┐
│     React Application (SPA)         │
│  - Hosted separately (Vite/Nginx)   │
│  - Client-side routing               │
│  - State management                  │
│  - API service layer                 │
└──────────────┬──────────────────────┘
               │ HTTP/JSON
               │ (CORS enabled)
               ▼
┌─────────────────────────────────────┐
│   ASP.NET Core REST API Backend     │
│  - API Controllers (/api/*)          │
│  - Entity Framework Core             │
│  - Business logic & validation       │
│  - File upload handling              │
│  - Notification service              │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│        SQL Server Database          │
│  - Existing schema maintained        │
└─────────────────────────────────────┘
```

### Technology Stack

**Frontend:**
- React 18+ with TypeScript
- Vite (build tool and dev server)
- React Router v6 (client-side routing)
- Axios (HTTP client)
- React Query (data fetching and caching)
- Material-UI (MUI) v5 (UI components)
- React Hook Form (form management)
- Yup (validation)

**Backend:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core (existing)
- CORS middleware
- File upload middleware
- Existing notification service

### Project Structure

```
/
├── ContosoUniversity/              # Existing ASP.NET backend
│   ├── Controllers/
│   │   └── Api/                    # New API controllers
│   │       ├── StudentsApiController.cs
│   │       ├── CoursesApiController.cs
│   │       ├── InstructorsApiController.cs
│   │       ├── DepartmentsApiController.cs
│   │       ├── NotificationsApiController.cs
│   │       └── HomeApiController.cs
│   ├── DTOs/                       # Data Transfer Objects
│   │   ├── StudentDto.cs
│   │   ├── CourseDto.cs
│   │   ├── InstructorDto.cs
│   │   ├── DepartmentDto.cs
│   │   └── PaginatedResponseDto.cs
│   ├── Models/                     # Existing models
│   ├── Data/                       # Existing data context
│   ├── Services/                   # Existing services
│   └── Program.cs                  # Updated with CORS and API routing
│
└── contoso-university-ui/          # New React application
    ├── public/
    ├── src/
    │   ├── api/                    # API service layer
    │   │   ├── client.ts           # Axios instance configuration
    │   │   ├── students.ts
    │   │   ├── courses.ts
    │   │   ├── instructors.ts
    │   │   ├── departments.ts
    │   │   └── notifications.ts
    │   ├── components/             # Reusable components
    │   │   ├── common/
    │   │   │   ├── Layout.tsx
    │   │   │   ├── Navigation.tsx
    │   │   │   ├── Pagination.tsx
    │   │   │   ├── LoadingSpinner.tsx
    │   │   │   ├── ErrorBoundary.tsx
    │   │   │   ├── ConfirmDialog.tsx
    │   │   │   └── MuiTheme.tsx      # Material-UI theme configuration
    │   │   ├── forms/
    │   │   │   ├── StudentForm.tsx
    │   │   │   ├── CourseForm.tsx
    │   │   │   ├── InstructorForm.tsx
    │   │   │   └── DepartmentForm.tsx
    │   │   └── tables/
    │   │       ├── StudentTable.tsx
    │   │       ├── CourseTable.tsx
    │   │       └── InstructorTable.tsx
    │   ├── pages/                  # Page components
    │   │   ├── Home.tsx
    │   │   ├── About.tsx
    │   │   ├── students/
    │   │   │   ├── StudentList.tsx
    │   │   │   ├── StudentDetails.tsx
    │   │   │   ├── StudentCreate.tsx
    │   │   │   ├── StudentEdit.tsx
    │   │   │   └── StudentDelete.tsx
    │   │   ├── courses/
    │   │   ├── instructors/
    │   │   ├── departments/
    │   │   └── notifications/
    │   ├── hooks/                  # Custom React hooks
    │   │   ├── useStudents.ts
    │   │   ├── useCourses.ts
    │   │   └── usePagination.ts
    │   ├── types/                  # TypeScript type definitions
    │   │   ├── student.ts
    │   │   ├── course.ts
    │   │   ├── instructor.ts
    │   │   └── api.ts
    │   ├── utils/                  # Utility functions
    │   │   ├── formatters.ts
    │   │   ├── validators.ts
    │   │   └── constants.ts
    │   ├── App.tsx                 # Main app component
    │   ├── main.tsx                # Entry point
    │   └── router.tsx              # Route configuration
    ├── .env.development            # Dev API URL
    ├── .env.production             # Prod API URL
    ├── package.json
    ├── tsconfig.json
    └── vite.config.ts
```

## Components and Interfaces

### Backend API Controllers

#### Base API Response Structure

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
```

#### Students API Controller

**Endpoints:**
- `GET /api/students` - Get paginated list with optional search and sorting
  - Query params: `page`, `pageSize`, `sortOrder`, `searchString`
  - Returns: `PaginatedResponse<StudentDto>`
- `GET /api/students/{id}` - Get student details with enrollments
  - Returns: `ApiResponse<StudentDto>`
- `POST /api/students` - Create new student
  - Body: `StudentCreateDto`
  - Returns: `ApiResponse<StudentDto>`
- `PUT /api/students/{id}` - Update student
  - Body: `StudentUpdateDto`
  - Returns: `ApiResponse<StudentDto>`
- `DELETE /api/students/{id}` - Delete student
  - Returns: `ApiResponse<bool>`

#### Courses API Controller

**Endpoints:**
- `GET /api/courses` - Get all courses with departments
- `GET /api/courses/{id}` - Get course details
- `POST /api/courses` - Create course with optional file upload
  - Content-Type: `multipart/form-data`
- `PUT /api/courses/{id}` - Update course with optional file upload
- `DELETE /api/courses/{id}` - Delete course and associated files

#### Instructors API Controller

**Endpoints:**
- `GET /api/instructors` - Get instructors with courses and office assignments
  - Query params: `instructorId`, `courseId` (for filtering)
- `GET /api/instructors/{id}` - Get instructor details
- `POST /api/instructors` - Create instructor with course assignments
- `PUT /api/instructors/{id}` - Update instructor
- `DELETE /api/instructors/{id}` - Delete instructor

#### Departments API Controller

**Endpoints:**
- `GET /api/departments` - Get all departments with administrators
- `GET /api/departments/{id}` - Get department details
- `POST /api/departments` - Create department
- `PUT /api/departments/{id}` - Update department (with concurrency handling)
- `DELETE /api/departments/{id}` - Delete department

#### Home API Controller

**Endpoints:**
- `GET /api/home/enrollment-stats` - Get enrollment statistics by date

#### Notifications API Controller

**Endpoints:**
- `GET /api/notifications` - Get all notifications
- `GET /api/notifications/{id}` - Get notification details
- `POST /api/notifications/mark-read/{id}` - Mark notification as read

### Frontend Components

#### Layout Components

**Navigation Component:**
- Renders top navigation bar with links to all sections
- Highlights active route
- Responsive mobile menu

**Layout Component:**
- Wraps all pages with consistent header, navigation, and footer
- Provides error boundary
- Manages global loading state

**Pagination Component:**
- Reusable pagination controls
- Props: `currentPage`, `totalPages`, `onPageChange`, `hasNext`, `hasPrevious`
- Displays page numbers and previous/next buttons

#### Page Components

**StudentList Page:**
- Displays paginated table of students
- Search input with debouncing
- Sortable columns (name, enrollment date)
- Action buttons (view, edit, delete)
- Create new student button

**StudentForm Component:**
- Reusable form for create and edit
- Form validation with error display
- Date picker for enrollment date
- Submit and cancel buttons

**CourseForm Component:**
- Form fields for course data
- Department dropdown
- File upload input with preview
- File validation (type and size)

**InstructorForm Component:**
- Basic instructor information fields
- Multi-select for course assignments
- Optional office assignment section

**DepartmentForm Component:**
- Department fields with validation
- Instructor dropdown for administrator
- Budget and date inputs
- Concurrency conflict handling

## Data Models

### DTOs (Data Transfer Objects)

```csharp
public class StudentDto
{
    public int ID { get; set; }
    public string LastName { get; set; }
    public string FirstMidName { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public List<EnrollmentDto> Enrollments { get; set; }
}

public class StudentCreateDto
{
    [Required]
    public string LastName { get; set; }
    [Required]
    public string FirstMidName { get; set; }
    [Required]
    public DateTime EnrollmentDate { get; set; }
}

public class CourseDto
{
    public int CourseID { get; set; }
    public string Title { get; set; }
    public int Credits { get; set; }
    public int DepartmentID { get; set; }
    public string DepartmentName { get; set; }
    public string TeachingMaterialImagePath { get; set; }
}

public class InstructorDto
{
    public int ID { get; set; }
    public string LastName { get; set; }
    public string FirstMidName { get; set; }
    public string FullName { get; set; }
    public DateTime HireDate { get; set; }
    public OfficeAssignmentDto OfficeAssignment { get; set; }
    public List<CourseAssignmentDto> CourseAssignments { get; set; }
}

public class DepartmentDto
{
    public int DepartmentID { get; set; }
    public string Name { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public int? InstructorID { get; set; }
    public string AdministratorName { get; set; }
    public byte[] RowVersion { get; set; }
}
```

### TypeScript Types

```typescript
export interface Student {
  id: number;
  lastName: string;
  firstMidName: string;
  enrollmentDate: string;
  enrollments?: Enrollment[];
}

export interface Course {
  courseID: number;
  title: string;
  credits: number;
  departmentID: number;
  departmentName?: string;
  teachingMaterialImagePath?: string;
}

export interface Instructor {
  id: number;
  lastName: string;
  firstMidName: string;
  fullName: string;
  hireDate: string;
  officeAssignment?: OfficeAssignment;
  courseAssignments: CourseAssignment[];
}

export interface PaginatedResponse<T> {
  items: T[];
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}
```

## Error Handling

### Backend Error Handling

**Global Exception Handler:**
- Implement middleware to catch unhandled exceptions
- Log errors with stack traces
- Return consistent error response format
- Map exception types to appropriate HTTP status codes

**Validation Error Handling:**
- Use ModelState validation
- Return 400 Bad Request with validation errors
- Format: `{ "success": false, "errors": ["Field: Error message"] }`

**Concurrency Error Handling:**
- Catch `DbUpdateConcurrencyException`
- Return 409 Conflict with current database values
- Include conflict details in response

### Frontend Error Handling

**Error Boundary Component:**
- Wrap application in error boundary
- Catch React component errors
- Display user-friendly error page
- Log errors to console (or error tracking service)

**API Error Handling:**
- Centralized error handling in API client
- Display toast notifications for errors
- Show inline validation errors in forms
- Handle network errors and timeouts
- Retry logic for transient failures

**HTTP Status Code Handling:**
- 400: Display validation errors
- 401: Redirect to login
- 403: Show "Access Denied" message
- 404: Show "Not Found" page
- 409: Display concurrency conflict dialog
- 500: Show generic error message

## Testing Strategy

### Backend Testing

**Unit Tests:**
- Test API controller actions
- Test DTO mapping logic
- Test validation rules
- Mock database context and services

**Integration Tests:**
- Test API endpoints end-to-end
- Use in-memory database
- Test file upload functionality
- Test CORS configuration

### Frontend Testing

**Unit Tests:**
- Test utility functions
- Test custom hooks
- Test form validation logic

**Component Tests:**
- Test component rendering
- Test user interactions
- Test form submissions
- Mock API calls

**Integration Tests:**
- Test page flows (create, edit, delete)
- Test navigation
- Test error scenarios

**E2E Tests (Optional):**
- Test critical user journeys
- Use Playwright or Cypress
- Test against real backend

## API Communication

### Axios Configuration

```typescript
// src/api/client.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // For authentication cookies
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401) {
      // Redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### API Service Example

```typescript
// src/api/students.ts
import apiClient from './client';
import { Student, PaginatedResponse, ApiResponse } from '../types';

export const studentsApi = {
  getAll: (params: {
    page?: number;
    pageSize?: number;
    sortOrder?: string;
    searchString?: string;
  }) => {
    return apiClient.get<PaginatedResponse<Student>>('/students', { params });
  },

  getById: (id: number) => {
    return apiClient.get<ApiResponse<Student>>(`/students/${id}`);
  },

  create: (student: Partial<Student>) => {
    return apiClient.post<ApiResponse<Student>>('/students', student);
  },

  update: (id: number, student: Partial<Student>) => {
    return apiClient.put<ApiResponse<Student>>(`/students/${id}`, student);
  },

  delete: (id: number) => {
    return apiClient.delete<ApiResponse<boolean>>(`/students/${id}`);
  },
};
```

## CORS Configuration

### Backend CORS Setup

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173", // Vite dev server
            "http://localhost:3000", // Alternative dev port
            "https://contoso-university.netlify.app" // Production URL
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// ...

app.UseCors("ReactApp");
```

## Routing

### Backend API Routes

All API controllers will use the `[Route("api/[controller]")]` attribute and be placed in the `Controllers/Api` namespace.

### Frontend Routes

```typescript
// src/router.tsx
import { createBrowserRouter } from 'react-router-dom';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <ErrorPage />,
    children: [
      { index: true, element: <Home /> },
      { path: 'about', element: <About /> },
      {
        path: 'students',
        children: [
          { index: true, element: <StudentList /> },
          { path: 'create', element: <StudentCreate /> },
          { path: ':id', element: <StudentDetails /> },
          { path: ':id/edit', element: <StudentEdit /> },
          { path: ':id/delete', element: <StudentDelete /> },
        ],
      },
      // Similar structure for courses, instructors, departments
      { path: 'notifications', element: <NotificationList /> },
    ],
  },
]);
```

## File Upload Handling

### Backend Implementation

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromForm] CourseCreateDto courseDto, IFormFile teachingMaterialImage)
{
    if (teachingMaterialImage != null)
    {
        var validation = ValidateFile(teachingMaterialImage);
        if (!validation.IsValid)
        {
            return BadRequest(new ApiResponse<object> 
            { 
                Success = false, 
                Errors = validation.Errors 
            });
        }

        var filePath = await SaveFileAsync(teachingMaterialImage, courseDto.CourseID);
        courseDto.TeachingMaterialImagePath = filePath;
    }

    // Save course...
}
```

### Frontend Implementation

```typescript
const handleSubmit = async (data: CourseFormData) => {
  const formData = new FormData();
  formData.append('title', data.title);
  formData.append('credits', data.credits.toString());
  formData.append('departmentID', data.departmentID.toString());
  
  if (data.teachingMaterialImage) {
    formData.append('teachingMaterialImage', data.teachingMaterialImage);
  }

  await coursesApi.create(formData);
};
```

## State Management

### Approach

Use React Query for server state management:
- Automatic caching and refetching
- Loading and error states
- Optimistic updates
- Pagination support

### Example Usage

```typescript
// src/hooks/useStudents.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { studentsApi } from '../api/students';

export const useStudents = (params: StudentQueryParams) => {
  return useQuery({
    queryKey: ['students', params],
    queryFn: () => studentsApi.getAll(params),
  });
};

export const useCreateStudent = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: studentsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] });
    },
  });
};
```

## Authentication Integration

The application currently uses Windows Authentication. The React app will:

1. Include credentials with requests (`withCredentials: true`)
2. Handle 401 responses by redirecting to login
3. Display user information from API response
4. Support logout functionality

If migrating to token-based auth:
- Store JWT in localStorage or httpOnly cookie
- Include token in Authorization header
- Implement token refresh logic
- Add login/logout pages

## Deployment Strategy

### Frontend Deployment

**Development:**
- Run Vite dev server: `npm run dev`
- API URL: `http://localhost:5000/api`

**Production Build:**
```bash
npm run build
# Generates optimized static files in /dist
```

**Hosting Options:**
- AWS S3 + CloudFront (primary deployment target)
- AWS Amplify (alternative with CI/CD)

**Environment Configuration:**
- `.env.development`: `VITE_API_BASE_URL=http://localhost:5000/api`
- `.env.production`: `VITE_API_BASE_URL=https://api.contoso-university.com/api`

**AWS Deployment:**
- Build static files: `npm run build`
- Upload to S3 bucket configured for static website hosting
- Configure CloudFront distribution for CDN and HTTPS
- Set up Route 53 for custom domain (optional)
- Configure S3 bucket policy for CloudFront access

### Backend Deployment

- Deploy ASP.NET Core API to existing hosting (IIS, Azure App Service, etc.)
- Ensure CORS is configured for production React app URL
- Update connection strings and configuration
- Enable HTTPS

## Migration Strategy

### Phase 1: Backend API Development
1. Create API controllers alongside existing MVC controllers
2. Implement DTOs and mapping
3. Add CORS configuration
4. Test API endpoints

### Phase 2: React Application Setup
1. Initialize React project with Vite
2. Set up routing and layout
3. Configure API client
4. Implement authentication

### Phase 3: Feature Migration
1. Migrate one entity at a time (Students → Courses → Instructors → Departments)
2. Build and test each feature completely before moving to next
3. Maintain existing MVC views during migration

### Phase 4: Testing and Refinement
1. Integration testing
2. User acceptance testing
3. Performance optimization
4. Accessibility audit

### Phase 5: Deployment
1. Deploy React app to hosting service
2. Update backend CORS for production URL
3. Monitor and fix issues
4. Remove old MVC views (optional)
