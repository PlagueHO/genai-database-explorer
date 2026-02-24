import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listViews, getView, patchView, patchViewColumn } from '../api/viewsApi';
import type { UpdateEntityDescriptionRequest, UpdateColumnDescriptionRequest } from '../types/api';

export function useViewsList(offset = 0, limit = 50) {
  return useQuery({
    queryKey: ['views', { offset, limit }],
    queryFn: () => listViews(offset, limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useViewDetail(schema: string, name: string) {
  return useQuery({
    queryKey: ['views', schema, name],
    queryFn: () => getView(schema, name),
    staleTime: 5 * 60 * 1000,
    enabled: !!schema && !!name,
  });
}

export function usePatchView(schema: string, name: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateEntityDescriptionRequest) => patchView(schema, name, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['views', schema, name] });
      queryClient.invalidateQueries({ queryKey: ['views', { offset: undefined }], exact: false });
    },
  });
}

export function usePatchViewColumn(schema: string, viewName: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      columnName,
      body,
    }: {
      columnName: string;
      body: UpdateColumnDescriptionRequest;
    }) => patchViewColumn(schema, viewName, columnName, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['views', schema, viewName] });
    },
  });
}
