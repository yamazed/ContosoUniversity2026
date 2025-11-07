import apiClient from './client';
import type { Instructor, InstructorCreate, InstructorUpdate } from '../types/instructor';
import type { ApiResponse } from '../types/api';

export interface InstructorQueryParams {
  instructorId?: number;
  courseId?: number;
}

export const instructorsApi = {
  /**
   * Get all instructors with courses and office assignments
   * Supports filtering by instructorId and courseId query parameters
   */
  getAll: async (params: InstructorQueryParams = {}): Promise<Instructor[]> => {
    const response = await apiClient.get<ApiResponse<Instructor[]>>('/instructors', { params });
    return response.data.data;
  },

  /**
   * Get instructor details by ID
   */
  getById: async (id: number): Promise<Instructor> => {
    const response = await apiClient.get<ApiResponse<Instructor>>(`/instructors/${id}`);
    return response.data.data;
  },

  /**
   * Create a new instructor with course assignments
   */
  create: async (instructor: InstructorCreate): Promise<Instructor> => {
    const response = await apiClient.post<ApiResponse<Instructor>>('/instructors', instructor);
    return response.data.data;
  },

  /**
   * Update an existing instructor with course assignments
   */
  update: async (id: number, instructor: InstructorUpdate): Promise<Instructor> => {
    const response = await apiClient.put<ApiResponse<Instructor>>(`/instructors/${id}`, instructor);
    return response.data.data;
  },

  /**
   * Delete an instructor by ID
   * Handles department administrator cleanup
   */
  delete: async (id: number): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/instructors/${id}`);
    return response.data.data;
  },
};
