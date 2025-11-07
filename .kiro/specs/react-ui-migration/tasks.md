# Implementation Plan

- [x] 1. Set up backend API infrastructure
  - Create `Controllers/Api` folder for API controllers
  - Create `DTOs` folder for data transfer objects
  - Configure CORS in `Program.cs` to allow React app origin
  - Add API routing configuration with `/api` prefix
  - _Requirements: 2.1, 2.2, 10.1, 10.2, 10.3, 10.5_

- [x] 2. Create base API response models and DTOs
  - Create `ApiResponse<T>` generic class for consistent API responses
  - Create `PaginatedResponse<T>` class for paginated data
  - Create DTOs for Student (StudentDto, StudentCreateDto, StudentUpdateDto)
  - Create DTOs for Course (CourseDto, CourseCreateDto, CourseUpdateDto)
  - Create DTOs for Instructor (InstructorDto, InstructorCreateDto, InstructorUpdateDto)
  - Create DTOs for Department (DepartmentDto, DepartmentCreateDto, DepartmentUpdateDto)
  - Create DTOs for Notification (NotificationDto)
  - Create DTOs for supporting entities (EnrollmentDto, CourseAssignmentDto, OfficeAssignmentDto)
  - _Requirements: 2.2, 2.3_

- [x] 3. Implement Students API controller
  - Create `StudentsApiController` with GET endpoint for paginated list with search and sorting
  - Implement GET endpoint for student details by ID with enrollments
  - Implement POST endpoint for creating new student with validation
  - Implement PUT endpoint for updating student
  - Implement DELETE endpoint for deleting student
  - Maintain notification service integration for create, update, delete operations
  - Add proper error handling and return appropriate HTTP status codes
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Implement Courses API controller
  - Create `CoursesApiController` with GET endpoint for all courses with departments
  - Implement GET endpoint for course details by ID
  - Implement POST endpoint with multipart/form-data support for file upload
  - Add file validation (type: jpg, jpeg, png, gif, bmp; size: max 5MB)
  - Implement PUT endpoint with file upload support
  - Implement DELETE endpoint that removes associated uploaded files
  - Maintain notification service integration
  - _Requirements: 2.1, 2.2, 2.3, 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 5. Implement Instructors API controller
  - Create `InstructorsApiController` with GET endpoint for instructors with courses and office assignments
  - Add support for filtering by instructorId and courseId query parameters
  - Implement GET endpoint for instructor details by ID
  - Implement POST endpoint for creating instructor with course assignments
  - Implement PUT endpoint for updating instructor with course assignments
  - Implement DELETE endpoint that handles department administrator cleanup
  - Maintain notification service integration
  - _Requirements: 2.1, 2.2, 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 6. Implement Departments API controller
  - Create `DepartmentsApiController` with GET endpoint for all departments with administrators
  - Implement GET endpoint for department details by ID
  - Implement POST endpoint for creating department
  - Implement PUT endpoint with concurrency conflict handling (DbUpdateConcurrencyException)
  - Return 409 Conflict status with current database values on concurrency errors
  - Implement DELETE endpoint for deleting department
  - Maintain notification service integration
  - _Requirements: 2.1, 2.2, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [x] 7. Implement Home and Notifications API controllers
  - Create `HomeApiController` with GET endpoint for enrollment statistics grouped by date
  - Create `NotificationsApiController` with GET endpoint for all notifications
  - Implement GET endpoint for notification details by ID
  - Add POST endpoint for marking notification as read (if needed)
  - _Requirements: 2.1, 2.2, 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 8.4_

- [x] 8. Add global error handling middleware
  - Create exception handling middleware to catch unhandled exceptions
  - Log errors with sufficient detail for debugging
  - Return consistent error response format with status code, message, and details
  - Map common exception types to appropriate HTTP status codes
  - Handle validation errors from ModelState
  - _Requirements: 2.4, 13.3, 13.4_

- [x] 9. Initialize React application with Vite and TypeScript
  - Create new React project using Vite with TypeScript template: `npm create vite@latest contoso-university-ui -- --template react-ts`
  - Install core dependencies: React Router, Axios, React Query, Material-UI
  - Install form dependencies: React Hook Form, Yup
  - Set up project folder structure (api, components, pages, hooks, types, utils)
  - Create `.env.development` and `.env.production` files with API base URLs
  - Configure `vite.config.ts` for proxy and build settings
  - _Requirements: 1.1, 11.1, 11.4, 15.1, 15.2, 15.3_

- [x] 10. Set up Material-UI theme and common components
  - Create Material-UI theme configuration with Contoso University branding
  - Wrap app with ThemeProvider and CssBaseline
  - Create Layout component with AppBar, navigation drawer, and footer
  - Create Navigation component with links to all sections
  - Create LoadingSpinner component using MUI CircularProgress
  - Create ErrorBoundary component for catching React errors
  - Create ConfirmDialog component for delete confirmations
  - _Requirements: 1.1, 12.1, 12.2, 13.2_

- [x] 11. Configure API client and service layer
  - Create Axios instance in `src/api/client.ts` with base URL from environment variables
  - Configure request interceptor to add authentication token
  - Configure response interceptor to handle 401 errors and redirect to login
  - Set `withCredentials: true` for authentication cookies
  - Create TypeScript interfaces for API responses in `src/types/api.ts`
  - _Requirements: 1.3, 9.1, 9.2, 9.3, 10.4, 13.5_

- [x] 12. Create TypeScript type definitions
  - Create `src/types/student.ts` with Student, StudentCreate, StudentUpdate interfaces
  - Create `src/types/course.ts` with Course, CourseCreate, CourseUpdate interfaces
  - Create `src/types/instructor.ts` with Instructor, InstructorCreate, InstructorUpdate interfaces
  - Create `src/types/department.ts` with Department, DepartmentCreate, DepartmentUpdate interfaces
  - Create `src/types/notification.ts` with Notification interface
  - Create `src/types/api.ts` with PaginatedResponse, ApiResponse, ApiError interfaces
  - _Requirements: 11.1_

- [x] 13. Implement Students API service and hooks
  - Create `src/api/students.ts` with functions for all student endpoints (getAll, getById, create, update, delete)
  - Create `src/hooks/useStudents.ts` with React Query hooks (useStudents, useStudent, useCreateStudent, useUpdateStudent, useDeleteStudent)
  - Configure query keys and cache invalidation
  - _Requirements: 1.3, 11.3, 11.4_

- [x] 14. Build Students pages and components
- [x] 14.1 Create StudentList page
  - Build StudentList page with MUI DataGrid or Table component
  - Implement search input with debouncing
  - Add sortable columns for name and enrollment date
  - Integrate pagination component with API pagination
  - Add action buttons (view, edit, delete) for each row
  - Add "Create New Student" button
  - Display loading spinner during data fetch
  - Handle and display errors
  - _Requirements: 1.1, 1.2, 3.1, 3.2, 12.5, 14.1, 14.2, 14.3, 14.4_

- [x] 14.2 Create StudentForm component
  - Build reusable form component with React Hook Form
  - Add form fields: LastName, FirstMidName, EnrollmentDate
  - Implement Yup validation schema for required fields and date validation
  - Add MUI DatePicker for enrollment date
  - Display validation errors inline
  - Add submit and cancel buttons
  - _Requirements: 1.3, 3.3, 12.3_

- [x] 14.3 Create StudentCreate page
  - Use StudentForm component for creating new students
  - Call useCreateStudent hook on form submission
  - Show success message and navigate to list on success
  - Handle and display API errors
  - _Requirements: 1.3, 3.3, 3.4_

- [x] 14.4 Create StudentEdit page
  - Fetch student data by ID using useStudent hook
  - Pre-populate StudentForm with existing data
  - Call useUpdateStudent hook on form submission
  - Handle and display API errors
  - _Requirements: 1.3, 3.3, 3.4_

- [x] 14.5 Create StudentDetails page
  - Fetch and display student details including enrollments
  - Show student information in MUI Card components
  - Display enrollment list with course information
  - Add navigation buttons (back, edit, delete)
  - _Requirements: 1.1, 3.5_

- [x] 14.6 Create StudentDelete page
  - Display student information for confirmation
  - Show ConfirmDialog for delete confirmation
  - Call useDeleteStudent hook on confirmation
  - Navigate to list on success
  - _Requirements: 3.4_

- [x] 15. Implement Courses API service and hooks
  - Create `src/api/courses.ts` with functions for all course endpoints
  - Handle multipart/form-data for file uploads
  - Create `src/hooks/useCourses.ts` with React Query hooks
  - _Requirements: 1.3, 11.3_

- [x] 16. Build Courses pages and components
- [x] 16.1 Create CourseList page
  - Build CourseList page with MUI Table displaying courses and departments
  - Add action buttons for each course
  - Add "Create New Course" button
  - _Requirements: 1.1, 4.1_

- [x] 16.2 Create CourseForm component
  - Build form with fields: CourseID, Title, Credits, DepartmentID
  - Add MUI Select dropdown for department selection
  - Add file upload input for teaching material image
  - Implement file validation (type and size) before submission
  - Show image preview when file is selected
  - Display validation errors
  - _Requirements: 1.3, 4.2, 4.3_

- [x] 16.3 Create CourseCreate page
  - Use CourseForm component
  - Handle file upload with FormData
  - Call useCreateCourse hook
  - _Requirements: 1.3, 4.2, 4.3_

- [x] 16.4 Create CourseEdit page
  - Fetch course data and pre-populate form
  - Show existing teaching material image if available
  - Allow replacing image with new upload
  - Call useUpdateCourse hook
  - _Requirements: 1.3, 4.2, 4.3_

- [x] 16.5 Create CourseDetails page
  - Display course information with department
  - Show teaching material image if available
  - Add navigation buttons
  - _Requirements: 1.1, 4.5_

- [x] 16.6 Create CourseDelete page
  - Display course information for confirmation
  - Show ConfirmDialog
  - Call useDeleteCourse hook
  - _Requirements: 4.4_

- [x] 17. Implement Instructors API service and hooks
  - Create `src/api/instructors.ts` with functions for all instructor endpoints
  - Support query parameters for filtering (instructorId, courseId)
  - Create `src/hooks/useInstructors.ts` with React Query hooks
  - _Requirements: 1.3, 11.3_

- [x] 18. Build Instructors pages and components
- [x] 18.1 Create InstructorList page
  - Build InstructorList page with master-detail layout
  - Display instructors list with office assignments
  - Show courses for selected instructor
  - Show enrollments for selected course
  - Implement selection state management
  - _Requirements: 1.1, 5.1, 5.2, 5.3_

- [x] 18.2 Create InstructorForm component
  - Build form with fields: LastName, FirstMidName, HireDate
  - Add optional office assignment section with location field
  - Add MUI multi-select or checkbox list for course assignments
  - Implement validation
  - _Requirements: 1.3, 5.4, 5.5_

- [x] 18.3 Create InstructorCreate page
  - Use InstructorForm component
  - Call useCreateInstructor hook
  - _Requirements: 1.3, 5.4_

- [x] 18.4 Create InstructorEdit page
  - Fetch instructor data with courses and office assignment
  - Pre-populate form
  - Call useUpdateInstructor hook
  - _Requirements: 1.3, 5.4, 5.5_

- [x] 18.5 Create InstructorDetails page
  - Display instructor information
  - Show assigned courses and office location
  - _Requirements: 1.1, 5.1_

- [x] 18.6 Create InstructorDelete page
  - Display instructor information for confirmation
  - Show ConfirmDialog
  - Call useDeleteInstructor hook
  - _Requirements: 1.3_

- [x] 19. Implement Departments API service and hooks
  - Create `src/api/departments.ts` with functions for all department endpoints
  - Create `src/hooks/useDepartments.ts` with React Query hooks
  - _Requirements: 1.3, 11.3_

- [x] 20. Build Departments pages and components
- [x] 20.1 Create DepartmentList page
  - Build DepartmentList page with MUI Table
  - Display departments with administrator names and budgets
  - Add action buttons
  - _Requirements: 1.1, 6.1_

- [x] 20.2 Create DepartmentForm component
  - Build form with fields: Name, Budget, StartDate, InstructorID
  - Add MUI Select dropdown for administrator selection
  - Add MUI DatePicker for start date
  - Implement validation for budget and dates
  - _Requirements: 1.3, 6.3, 6.4_

- [x] 20.3 Create DepartmentCreate page
  - Use DepartmentForm component
  - Call useCreateDepartment hook
  - _Requirements: 1.3, 6.3_

- [x] 20.4 Create DepartmentEdit page
  - Fetch department data and pre-populate form
  - Handle 409 Conflict response for concurrency errors
  - Display conflict dialog with current database values
  - Allow user to retry with updated values
  - Call useUpdateDepartment hook
  - _Requirements: 1.3, 6.2, 6.4, 6.5_

- [x] 20.5 Create DepartmentDetails page
  - Display department information
  - Show administrator name and budget
  - _Requirements: 1.1, 6.1_

- [x] 20.6 Create DepartmentDelete page
  - Display department information for confirmation
  - Show ConfirmDialog
  - Call useDeleteDepartment hook
  - _Requirements: 6.6_

- [x] 21. Build Home and About pages
  - Create Home page with welcome message and navigation cards to main sections
  - Create About page that fetches and displays enrollment statistics
  - Use MUI Card components for layout
  - Create chart or table visualization for enrollment data
  - _Requirements: 1.1, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 22. Implement Notifications feature
  - Create `src/api/notifications.ts` with notification endpoints
  - Create `src/hooks/useNotifications.ts` with React Query hooks
  - Create NotificationList page displaying notifications in MUI Table or List
  - Add filtering or sorting options
  - Implement auto-refresh or manual refresh button
  - Display entity type, operation, and timestamp for each notification
  - _Requirements: 1.3, 8.1, 8.2, 8.3, 8.5_

- [x] 23. Set up React Router and navigation
  - Configure React Router with routes for all pages
  - Create route configuration in `src/router.tsx`
  - Implement nested routes for entity CRUD operations
  - Add error page component for 404 and other routing errors
  - Update Navigation component with active route highlighting
  - Test all navigation paths
  - _Requirements: 1.2, 11.4_

- [ ] 27. Add loading states and error handling
  - Ensure all pages show loading spinner during data fetch
  - Implement toast notifications for success and error messages (using MUI Snackbar)
  - Add error boundaries around major sections
  - Handle network errors and timeouts gracefully
  - Display user-friendly error messages
  - _Requirements: 12.5, 13.1, 13.2, 13.5_

- [x] 28. Create reusable Pagination component
  - Build Pagination component using MUI Pagination
  - Accept props: currentPage, totalPages, onPageChange, hasNext, hasPrevious
  - Integrate with paginated API responses
  - Maintain page state in URL query parameters
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_
