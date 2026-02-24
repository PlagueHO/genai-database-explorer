import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getModel, reloadModel } from '../api/modelApi';

export function useModel() {
  return useQuery({
    queryKey: ['model'],
    queryFn: getModel,
    staleTime: 5 * 60 * 1000,
  });
}

export function useReloadModel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: reloadModel,
    onSuccess: () => {
      queryClient.invalidateQueries();
    },
  });
}
