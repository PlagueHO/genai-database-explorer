import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listTables, getTable, patchTable, patchTableColumn } from '../api/tablesApi';
import type { UpdateEntityDescriptionRequest, UpdateColumnDescriptionRequest } from '../types/api';

export function useTablesList(offset = 0, limit = 50) {
  return useQuery({
    queryKey: ['tables', { offset, limit }],
    queryFn: () => listTables(offset, limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useTableDetail(schema: string, name: string) {
  return useQuery({
    queryKey: ['tables', schema, name],
    queryFn: () => getTable(schema, name),
    staleTime: 5 * 60 * 1000,
    enabled: !!schema && !!name,
  });
}

export function usePatchTable(schema: string, name: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateEntityDescriptionRequest) => patchTable(schema, name, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tables', schema, name] });
      queryClient.invalidateQueries({ queryKey: ['tables', { offset: undefined }], exact: false });
    },
  });
}

export function usePatchTableColumn(schema: string, tableName: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      columnName,
      body,
    }: {
      columnName: string;
      body: UpdateColumnDescriptionRequest;
    }) => patchTableColumn(schema, tableName, columnName, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tables', schema, tableName] });
    },
  });
}
