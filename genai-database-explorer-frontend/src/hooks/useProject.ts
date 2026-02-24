import { useQuery } from '@tanstack/react-query';
import { getProject } from '../api/projectApi';

export function useProject() {
  return useQuery({
    queryKey: ['project'],
    queryFn: getProject,
    staleTime: 10 * 60 * 1000,
  });
}
