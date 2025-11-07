import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { departmentsApi } from '../api/departments';
import type { Department, DepartmentCreate, DepartmentUpdate } from '../types/department';

// Query keys for cache management
export const departmentKeys = {
  all: ['departments'] as const,
  lists: () => [...departmentKeys.all, 'list'] as const,
  list: () => [...departmentKeys.lists()] as const,
  details: () => [...departmentKeys.all, 'detail'] as const,
  detail: (id: number) => [...departmentKeys.details(), id] as const,
};

/**
 * Hook to fetch all departments with administrators
 */
export const useDepartments = (
  options?: Omit<UseQueryOptions<Department[]>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: departmentKeys.list(),
    queryFn: () => departmentsApi.getAll(),
    ...options,
  });
};

/**
 * Hook to fetch a single department by ID
 */
export const useDepartment = (
  id: number,
  options?: Omit<UseQueryOptions<Department>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: departmentKeys.detail(id),
    queryFn: () => departmentsApi.getById(id),
    enabled: !!id, // Only fetch if id is provided
    ...options,
  });
};

/**
 * Hook to create a new department
 * Automatically invalidates the departments list cache on success
 */
export const useCreateDepartment = (
  options?: Omit<UseMutationOptions<Department, Error, DepartmentCreate, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Department, Error, DepartmentCreate, unknown>({
    ...options,
    mutationFn: departmentsApi.create,
    onSuccess: async (...args) => {
      // Invalidate all department lists to refetch with new data
      await queryClient.invalidateQueries({ queryKey: departmentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(...args);
      }
    },
  });
};

/**
 * Hook to update an existing department
 * Automatically invalidates the department detail and lists cache on success
 * Handles concurrency conflicts (409 Conflict) with rowVersion
 */
export const useUpdateDepartment = (
  options?: Omit<UseMutationOptions<Department, Error, { id: number; data: DepartmentUpdate }, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Department, Error, { id: number; data: DepartmentUpdate }, unknown>({
    ...options,
    mutationFn: ({ id, data }) => departmentsApi.update(id, data),
    onSuccess: async (data, variables, ...args) => {
      // Invalidate the specific department detail
      await queryClient.invalidateQueries({ queryKey: departmentKeys.detail(variables.id) });
      
      // Invalidate all department lists to refetch with updated data
      await queryClient.invalidateQueries({ queryKey: departmentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};

/**
 * Hook to delete a department
 * Automatically invalidates the departments list cache on success
 */
export const useDeleteDepartment = (
  options?: Omit<UseMutationOptions<boolean, Error, number, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<boolean, Error, number, unknown>({
    ...options,
    mutationFn: departmentsApi.delete,
    onSuccess: async (data, variables, ...args) => {
      // Remove the specific department from cache
      queryClient.removeQueries({ queryKey: departmentKeys.detail(variables) });
      
      // Invalidate all department lists to refetch without deleted department
      await queryClient.invalidateQueries({ queryKey: departmentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};
