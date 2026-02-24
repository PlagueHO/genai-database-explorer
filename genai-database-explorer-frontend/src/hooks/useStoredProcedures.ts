import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  listStoredProcedures,
  getStoredProcedure,
  patchStoredProcedure,
} from '../api/storedProceduresApi';
import type { UpdateEntityDescriptionRequest } from '../types/api';

export function useStoredProceduresList(offset = 0, limit = 50) {
  return useQuery({
    queryKey: ['storedProcedures', { offset, limit }],
    queryFn: () => listStoredProcedures(offset, limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useStoredProcedureDetail(schema: string, name: string) {
  return useQuery({
    queryKey: ['storedProcedures', schema, name],
    queryFn: () => getStoredProcedure(schema, name),
    staleTime: 5 * 60 * 1000,
    enabled: !!schema && !!name,
  });
}

export function usePatchStoredProcedure(schema: string, name: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateEntityDescriptionRequest) => patchStoredProcedure(schema, name, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['storedProcedures', schema, name] });
      queryClient.invalidateQueries({
        queryKey: ['storedProcedures', { offset: undefined }],
        exact: false,
      });
    },
  });
}
