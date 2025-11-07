import apiClient from './client';
import type { Student, StudentCreate, StudentUpdate } from '../types/student';
import type { PaginatedResponse, ApiResponse } from '../types/api';

export interface StudentQueryParams {
  page?: number;
  pageSize?: number;
  sortOrder?: string;
  searchString?: string;
}

export const studentsApi = {
  /**
   * Get paginated list of students with optional search and sorting
   */
  getAll: async (params: StudentQueryParams = {}): Promise<PaginatedResponse<Student>> => {
    const response = await apiClient.get<PaginatedResponse<Student>>('/students', { params });
    return response.data;
  },

  /**
   * Get student details by ID with enrollments
   */
  getById: async (id: number): Promise<Student> => {
    const response = await apiClient.get<ApiResponse<Student>>(`/students/${id}`);
    return response.data.data;
  },

  /**
   * Create a new student
   */
  create: async (student: StudentCreate): Promise<Student> => {
    const response = await apiClient.post<ApiResponse<Student>>('/students', student);
    return response.data.data;
  },

  /**
   * Update an existing student
   */
  update: async (id: number, student: StudentUpdate): Promise<Student> => {
    const response = await apiClient.put<ApiResponse<Student>>(`/students/${id}`, student);
    return response.data.data;
  },

  /**
   * Delete a student by ID
   */
  delete: async (id: number): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/students/${id}`);
    return response.data.data;
  },
};
