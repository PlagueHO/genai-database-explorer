import type { ProjectInfo } from '../types/api';
import { apiGet } from './client';

export function getProject(): Promise<ProjectInfo> {
  return apiGet<ProjectInfo>('/api/project');
}
