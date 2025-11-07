import apiClient from './client';
import type { ApiResponse } from '../types';

export interface EnrollmentStatistic {
  enrollmentDate: string | null;
  studentCount: number;
}

export const homeApi = {
  getEnrollmentStatistics: async () => {
    const response = await apiClient.get<ApiResponse<EnrollmentStatistic[]>>('/home/enrollment-stats');
    return response.data;
  },
};
