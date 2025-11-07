import apiClient from './client';
import type { Course, CourseCreate, CourseUpdate } from '../types/course';
import type { ApiResponse } from '../types/api';

export const coursesApi = {
  /**
   * Get all courses with departments
   */
  getAll: async (): Promise<Course[]> => {
    const response = await apiClient.get<ApiResponse<Course[]>>('/courses');
    return response.data.data;
  },

  /**
   * Get course details by ID
   */
  getById: async (id: number): Promise<Course> => {
    const response = await apiClient.get<ApiResponse<Course>>(`/courses/${id}`);
    return response.data.data;
  },

  /**
   * Create a new course with optional file upload
   * Handles multipart/form-data for teaching material image
   */
  create: async (course: CourseCreate): Promise<Course> => {
    const formData = new FormData();
    
    // Append course data
    formData.append('courseID', course.courseID.toString());
    formData.append('title', course.title);
    formData.append('credits', course.credits.toString());
    formData.append('departmentID', course.departmentID.toString());
    
    // Append file if provided
    if (course.teachingMaterialImage) {
      formData.append('teachingMaterialImage', course.teachingMaterialImage);
    }

    const response = await apiClient.post<ApiResponse<Course>>('/courses', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data.data;
  },

  /**
   * Update an existing course with optional file upload
   * Handles multipart/form-data for teaching material image
   */
  update: async (id: number, course: CourseUpdate): Promise<Course> => {
    const formData = new FormData();
    
    // Append course data
    formData.append('courseID', course.courseID.toString());
    formData.append('title', course.title);
    formData.append('credits', course.credits.toString());
    formData.append('departmentID', course.departmentID.toString());
    
    // Append file if provided
    if (course.teachingMaterialImage) {
      formData.append('teachingMaterialImage', course.teachingMaterialImage);
    }

    const response = await apiClient.put<ApiResponse<Course>>(`/courses/${id}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data.data;
  },

  /**
   * Delete a course by ID
   * Also removes associated uploaded files
   */
  delete: async (id: number): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/courses/${id}`);
    return response.data.data;
  },
};
