import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { instructorsApi, type InstructorQueryParams } from '../api/instructors';
import type { Instructor, InstructorCreate, InstructorUpdate } from '../types/instructor';

// Query keys for cache management
export const instructorKeys = {
  all: ['instructors'] as const,
  lists: () => [...instructorKeys.all, 'list'] as const,
  list: (params: InstructorQueryParams) => [...instructorKeys.lists(), params] as const,
  details: () => [...instructorKeys.all, 'detail'] as const,
  detail: (id: number) => [...instructorKeys.details(), id] as const,
};

/**
 * Hook to fetch all instructors with courses and office assignments
 * Supports filtering by instructorId and courseId query parameters
 */
export const useInstructors = (
  params: InstructorQueryParams = {},
  options?: Omit<UseQueryOptions<Instructor[]>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: instructorKeys.list(params),
    queryFn: () => instructorsApi.getAll(params),
    ...options,
  });
};

/**
 * Hook to fetch a single instructor by ID
 */
export const useInstructor = (
  id: number,
  options?: Omit<UseQueryOptions<Instructor>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: instructorKeys.detail(id),
    queryFn: () => instructorsApi.getById(id),
    enabled: !!id, // Only fetch if id is provided
    ...options,
  });
};

/**
 * Hook to create a new instructor with course assignments
 * Automatically invalidates the instructors list cache on success
 */
export const useCreateInstructor = (
  options?: Omit<UseMutationOptions<Instructor, Error, InstructorCreate, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Instructor, Error, InstructorCreate, unknown>({
    ...options,
    mutationFn: instructorsApi.create,
    onSuccess: async (...args) => {
      // Invalidate all instructor lists to refetch with new data
      await queryClient.invalidateQueries({ queryKey: instructorKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(...args);
      }
    },
  });
};

/**
 * Hook to update an existing instructor with course assignments
 * Automatically invalidates the instructor detail and lists cache on success
 */
export const useUpdateInstructor = (
  options?: Omit<UseMutationOptions<Instructor, Error, { id: number; data: InstructorUpdate }, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Instructor, Error, { id: number; data: InstructorUpdate }, unknown>({
    ...options,
    mutationFn: ({ id, data }) => instructorsApi.update(id, data),
    onSuccess: async (data, variables, ...args) => {
      // Invalidate the specific instructor detail
      await queryClient.invalidateQueries({ queryKey: instructorKeys.detail(variables.id) });
      
      // Invalidate all instructor lists to refetch with updated data
      await queryClient.invalidateQueries({ queryKey: instructorKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};

/**
 * Hook to delete an instructor
 * Automatically invalidates the instructors list cache on success
 */
export const useDeleteInstructor = (
  options?: Omit<UseMutationOptions<boolean, Error, number, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<boolean, Error, number, unknown>({
    ...options,
    mutationFn: instructorsApi.delete,
    onSuccess: async (data, variables, ...args) => {
      // Remove the specific instructor from cache
      queryClient.removeQueries({ queryKey: instructorKeys.detail(variables) });
      
      // Invalidate all instructor lists to refetch without deleted instructor
      await queryClient.invalidateQueries({ queryKey: instructorKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};
