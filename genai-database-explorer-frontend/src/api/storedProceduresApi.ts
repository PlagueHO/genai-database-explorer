import type {
  PaginatedResponse,
  EntitySummary,
  StoredProcedureDetail,
  UpdateEntityDescriptionRequest,
} from '../types/api';
import { apiGet, apiPatch } from './client';

export function listStoredProcedures(
  offset = 0,
  limit = 50,
): Promise<PaginatedResponse<EntitySummary>> {
  return apiGet<PaginatedResponse<EntitySummary>>(
    `/api/stored-procedures?offset=${offset}&limit=${limit}`,
  );
}

export function getStoredProcedure(schema: string, name: string): Promise<StoredProcedureDetail> {
  return apiGet<StoredProcedureDetail>(
    `/api/stored-procedures/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`,
  );
}

export function patchStoredProcedure(
  schema: string,
  name: string,
  body: UpdateEntityDescriptionRequest,
): Promise<StoredProcedureDetail> {
  return apiPatch<StoredProcedureDetail>(
    `/api/stored-procedures/${encodeURIComponent(schema)}/${encodeURIComponent(name)}`,
    body,
  );
}
