import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { studentsApi, type StudentQueryParams } from '../api/students';
import type { Student, StudentCreate, StudentUpdate } from '../types/student';
import type { PaginatedResponse } from '../types/api';

// Query keys for cache management
export const studentKeys = {
  all: ['students'] as const,
  lists: () => [...studentKeys.all, 'list'] as const,
  list: (params: StudentQueryParams) => [...studentKeys.lists(), params] as const,
  details: () => [...studentKeys.all, 'detail'] as const,
  detail: (id: number) => [...studentKeys.details(), id] as const,
};

/**
 * Hook to fetch paginated list of students with optional search and sorting
 */
export const useStudents = (
  params: StudentQueryParams = {},
  options?: Omit<UseQueryOptions<PaginatedResponse<Student>>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: studentKeys.list(params),
    queryFn: () => studentsApi.getAll(params),
    ...options,
  });
};

/**
 * Hook to fetch a single student by ID with enrollments
 */
export const useStudent = (
  id: number,
  options?: Omit<UseQueryOptions<Student>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: studentKeys.detail(id),
    queryFn: () => studentsApi.getById(id),
    enabled: !!id, // Only fetch if id is provided
    ...options,
  });
};

/**
 * Hook to create a new student
 * Automatically invalidates the students list cache on success
 */
export const useCreateStudent = (
  options?: Omit<UseMutationOptions<Student, Error, StudentCreate, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Student, Error, StudentCreate, unknown>({
    ...options,
    mutationFn: studentsApi.create,
    onSuccess: async (...args) => {
      // Invalidate all student lists to refetch with new data
      await queryClient.invalidateQueries({ queryKey: studentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(...args);
      }
    },
  });
};

/**
 * Hook to update an existing student
 * Automatically invalidates the student detail and lists cache on success
 */
export const useUpdateStudent = (
  options?: Omit<UseMutationOptions<Student, Error, { id: number; data: StudentUpdate }, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Student, Error, { id: number; data: StudentUpdate }, unknown>({
    ...options,
    mutationFn: ({ id, data }) => studentsApi.update(id, data),
    onSuccess: async (data, variables, ...args) => {
      // Invalidate the specific student detail
      await queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.id) });
      
      // Invalidate all student lists to refetch with updated data
      await queryClient.invalidateQueries({ queryKey: studentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};

/**
 * Hook to delete a student
 * Automatically invalidates the students list cache on success
 */
export const useDeleteStudent = (
  options?: Omit<UseMutationOptions<boolean, Error, number, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<boolean, Error, number, unknown>({
    ...options,
    mutationFn: studentsApi.delete,
    onSuccess: async (data, variables, ...args) => {
      // Remove the specific student from cache
      queryClient.removeQueries({ queryKey: studentKeys.detail(variables) });
      
      // Invalidate all student lists to refetch without deleted student
      await queryClient.invalidateQueries({ queryKey: studentKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
    ...options,
  });
};
