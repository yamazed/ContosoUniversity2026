import apiClient from './client';
import type { Department, DepartmentCreate, DepartmentUpdate } from '../types/department';
import type { ApiResponse } from '../types/api';

export const departmentsApi = {
  /**
   * Get all departments with administrators
   */
  getAll: async (): Promise<Department[]> => {
    const response = await apiClient.get<ApiResponse<Department[]>>('/departments');
    return response.data.data;
  },

  /**
   * Get department details by ID
   */
  getById: async (id: number): Promise<Department> => {
    const response = await apiClient.get<ApiResponse<Department>>(`/departments/${id}`);
    return response.data.data;
  },

  /**
   * Create a new department
   */
  create: async (department: DepartmentCreate): Promise<Department> => {
    const response = await apiClient.post<ApiResponse<Department>>('/departments', department);
    return response.data.data;
  },

  /**
   * Update an existing department
   * Handles concurrency conflicts (409 Conflict) with rowVersion
   */
  update: async (id: number, department: DepartmentUpdate): Promise<Department> => {
    const response = await apiClient.put<ApiResponse<Department>>(`/departments/${id}`, department);
    return response.data.data;
  },

  /**
   * Delete a department by ID
   */
  delete: async (id: number): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/departments/${id}`);
    return response.data.data;
  },
};
