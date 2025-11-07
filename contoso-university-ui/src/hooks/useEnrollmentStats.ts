import { useQuery } from '@tanstack/react-query';
import { homeApi } from '../api';

export const useEnrollmentStats = () => {
  return useQuery({
    queryKey: ['enrollmentStats'],
    queryFn: homeApi.getEnrollmentStatistics,
  });
};
