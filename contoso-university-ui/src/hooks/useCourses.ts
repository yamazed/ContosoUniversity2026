import { useQuery, useMutation, useQueryClient, type UseQueryOptions, type UseMutationOptions } from '@tanstack/react-query';
import { coursesApi } from '../api/courses';
import type { Course, CourseCreate, CourseUpdate } from '../types/course';

// Query keys for cache management
export const courseKeys = {
  all: ['courses'] as const,
  lists: () => [...courseKeys.all, 'list'] as const,
  list: () => [...courseKeys.lists()] as const,
  details: () => [...courseKeys.all, 'detail'] as const,
  detail: (id: number) => [...courseKeys.details(), id] as const,
};

/**
 * Hook to fetch all courses with departments
 */
export const useCourses = (
  options?: Omit<UseQueryOptions<Course[]>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: courseKeys.list(),
    queryFn: () => coursesApi.getAll(),
    ...options,
  });
};

/**
 * Hook to fetch a single course by ID
 */
export const useCourse = (
  id: number,
  options?: Omit<UseQueryOptions<Course>, 'queryKey' | 'queryFn'>
) => {
  return useQuery({
    queryKey: courseKeys.detail(id),
    queryFn: () => coursesApi.getById(id),
    enabled: !!id, // Only fetch if id is provided
    ...options,
  });
};

/**
 * Hook to create a new course with optional file upload
 * Automatically invalidates the courses list cache on success
 */
export const useCreateCourse = (
  options?: Omit<UseMutationOptions<Course, Error, CourseCreate, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Course, Error, CourseCreate, unknown>({
    ...options,
    mutationFn: coursesApi.create,
    onSuccess: async (...args) => {
      // Invalidate all course lists to refetch with new data
      await queryClient.invalidateQueries({ queryKey: courseKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(...args);
      }
    },
  });
};

/**
 * Hook to update an existing course with optional file upload
 * Automatically invalidates the course detail and lists cache on success
 */
export const useUpdateCourse = (
  options?: Omit<UseMutationOptions<Course, Error, { id: number; data: CourseUpdate }, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<Course, Error, { id: number; data: CourseUpdate }, unknown>({
    ...options,
    mutationFn: ({ id, data }) => coursesApi.update(id, data),
    onSuccess: async (data, variables, ...args) => {
      // Invalidate the specific course detail
      await queryClient.invalidateQueries({ queryKey: courseKeys.detail(variables.id) });
      
      // Invalidate all course lists to refetch with updated data
      await queryClient.invalidateQueries({ queryKey: courseKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};

/**
 * Hook to delete a course
 * Automatically invalidates the courses list cache on success
 */
export const useDeleteCourse = (
  options?: Omit<UseMutationOptions<boolean, Error, number, unknown>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();

  return useMutation<boolean, Error, number, unknown>({
    ...options,
    mutationFn: coursesApi.delete,
    onSuccess: async (data, variables, ...args) => {
      // Remove the specific course from cache
      queryClient.removeQueries({ queryKey: courseKeys.detail(variables) });
      
      // Invalidate all course lists to refetch without deleted course
      await queryClient.invalidateQueries({ queryKey: courseKeys.lists() });
      
      // Call custom onSuccess if provided
      if (options?.onSuccess) {
        await options.onSuccess(data, variables, ...args);
      }
    },
  });
};
