import { createBrowserRouter, Navigate } from 'react-router-dom';
import { Layout } from './components/common';
import { ErrorPage } from './pages/ErrorPage';
import { Home } from './pages/Home';
import { About } from './pages/About';

// Student pages
import { StudentList } from './pages/students/StudentList';
import { StudentCreate } from './pages/students/StudentCreate';
import { StudentEdit } from './pages/students/StudentEdit';
import { StudentDetails } from './pages/students/StudentDetails';
import { StudentDelete } from './pages/students/StudentDelete';

// Course pages
import { CourseList } from './pages/courses/CourseList';
import { CourseCreate } from './pages/courses/CourseCreate';
import { CourseEdit } from './pages/courses/CourseEdit';
import { CourseDetails } from './pages/courses/CourseDetails';
import { CourseDelete } from './pages/courses/CourseDelete';

// Instructor pages
import { InstructorList } from './pages/instructors/InstructorList';
import { InstructorCreate } from './pages/instructors/InstructorCreate';
import { InstructorEdit } from './pages/instructors/InstructorEdit';
import { InstructorDetails } from './pages/instructors/InstructorDetails';
import { InstructorDelete } from './pages/instructors/InstructorDelete';

// Department pages
import { DepartmentList } from './pages/departments/DepartmentList';
import { DepartmentCreate } from './pages/departments/DepartmentCreate';
import { DepartmentEdit } from './pages/departments/DepartmentEdit';
import { DepartmentDetails } from './pages/departments/DepartmentDetails';
import { DepartmentDelete } from './pages/departments/DepartmentDelete';

// Notification pages
import { NotificationList } from './pages/notifications/NotificationList';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <ErrorPage />,
    children: [
      {
        index: true,
        element: <Home />,
      },
      {
        path: 'about',
        element: <About />,
      },
      {
        path: 'students',
        children: [
          {
            index: true,
            element: <StudentList />,
          },
          {
            path: 'create',
            element: <StudentCreate />,
          },
          {
            path: ':id',
            element: <StudentDetails />,
          },
          {
            path: ':id/edit',
            element: <StudentEdit />,
          },
          {
            path: ':id/delete',
            element: <StudentDelete />,
          },
        ],
      },
      {
        path: 'courses',
        children: [
          {
            index: true,
            element: <CourseList />,
          },
          {
            path: 'create',
            element: <CourseCreate />,
          },
          {
            path: ':id',
            element: <CourseDetails />,
          },
          {
            path: ':id/edit',
            element: <CourseEdit />,
          },
          {
            path: ':id/delete',
            element: <CourseDelete />,
          },
        ],
      },
      {
        path: 'instructors',
        children: [
          {
            index: true,
            element: <InstructorList />,
          },
          {
            path: 'create',
            element: <InstructorCreate />,
          },
          {
            path: ':id',
            element: <InstructorDetails />,
          },
          {
            path: ':id/edit',
            element: <InstructorEdit />,
          },
          {
            path: ':id/delete',
            element: <InstructorDelete />,
          },
        ],
      },
      {
        path: 'departments',
        children: [
          {
            index: true,
            element: <DepartmentList />,
          },
          {
            path: 'create',
            element: <DepartmentCreate />,
          },
          {
            path: ':id',
            element: <DepartmentDetails />,
          },
          {
            path: ':id/edit',
            element: <DepartmentEdit />,
          },
          {
            path: ':id/delete',
            element: <DepartmentDelete />,
          },
        ],
      },
      {
        path: 'notifications',
        element: <NotificationList />,
      },
      {
        path: '*',
        element: <Navigate to="/" replace />,
      },
    ],
  },
]);
