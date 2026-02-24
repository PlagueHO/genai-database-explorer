import type { SemanticModelSummary } from '../types/api';
import { apiGet, apiPost } from './client';

export function getModel(): Promise<SemanticModelSummary> {
  return apiGet<SemanticModelSummary>('/api/model');
}

export function reloadModel(): Promise<SemanticModelSummary> {
  return apiPost<SemanticModelSummary>('/api/model/reload');
}
