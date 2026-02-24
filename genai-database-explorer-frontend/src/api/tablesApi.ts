import type {
  PaginatedResponse,
  EntitySummary,
  TableDetail,
  UpdateEntityDescriptionRequest,
  UpdateColumnDescriptionRequest,
  Column,
} from '../types/api';
import { apiGet, apiPatch } from './client';

export function listTables(offset = 0, limit = 50): Promise<PaginatedResponse<EntitySummary>> {
  return apiGet<PaginatedResponse<EntitySummary>>(`/api/tables?offset=${offset}&limit=${limit}`);
}

export function getTable(schema: string, name: string): Promise<TableDetail> {
  return apiGet<TableDetail>(
    `/api/tables/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`,
  );
}

export function patchTable(
  schema: string,
  name: string,
  body: UpdateEntityDescriptionRequest,
): Promise<TableDetail> {
  return apiPatch<TableDetail>(
    `/api/tables/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`,
    body,
  );
}

export function patchTableColumn(
  schema: string,
  tableName: string,
  columnName: string,
  body: UpdateColumnDescriptionRequest,
): Promise<Column> {
  return apiPatch<Column>(
    `/api/tables/${encodeURIComponent(schema)}/${encodeURIComponent(tableName)}/columns/${encodeURIComponent(columnName)}`,
    body,
  );
}
