import type {
  PaginatedResponse,
  EntitySummary,
  ViewDetail,
  UpdateEntityDescriptionRequest,
  UpdateColumnDescriptionRequest,
  Column,
} from '../types/api';
import { apiGet, apiPatch } from './client';

export function listViews(offset = 0, limit = 50): Promise<PaginatedResponse<EntitySummary>> {
  return apiGet<PaginatedResponse<EntitySummary>>(`/api/views?offset=${offset}&limit=${limit}`);
}

export function getView(schema: string, name: string): Promise<ViewDetail> {
  return apiGet<ViewDetail>(`/api/views/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`);
}

export function patchView(
  schema: string,
  name: string,
  body: UpdateEntityDescriptionRequest,
): Promise<ViewDetail> {
  return apiPatch<ViewDetail>(
    `/api/views/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`,
    body,
  );
}

export function patchViewColumn(
  schema: string,
  viewName: string,
  columnName: string,
  body: UpdateColumnDescriptionRequest,
): Promise<Column> {
  return apiPatch<Column>(
    `/api/views/${encodeURIComponent(schema)}/${encodeURIComponent(viewName)}/columns/${encodeURIComponent(columnName)}`,
    body,
  );
}
