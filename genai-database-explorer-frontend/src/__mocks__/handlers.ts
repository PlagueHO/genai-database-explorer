import { http, HttpResponse } from 'msw';
import {
  mockProject,
  mockModelSummary,
  mockTableSummaries,
  mockViewSummaries,
  mockStoredProcedureSummaries,
  mockTableDetails,
  mockViewDetails,
  mockStoredProcedureDetails,
} from './data';

export const handlers = [
  // Project
  http.get('/api/project', () => HttpResponse.json(mockProject)),

  // Model
  http.get('/api/model', () => HttpResponse.json(mockModelSummary)),
  http.post('/api/model/reload', () => HttpResponse.json(mockModelSummary)),

  // Tables
  http.get('/api/tables', ({ request }) => {
    const url = new URL(request.url);
    const offset = Number(url.searchParams.get('offset') ?? '0');
    const limit = Number(url.searchParams.get('limit') ?? '50');
    const page = mockTableSummaries.slice(offset, offset + limit);
    return HttpResponse.json({
      items: page,
      totalCount: mockTableSummaries.length,
      offset,
      limit,
    });
  }),
  http.get('/api/tables/:schema/:name', ({ params }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockTableDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json(detail);
  }),
  http.patch('/api/tables/:schema/:name', async ({ params, request }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockTableDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as Record<string, unknown>;
    const updated = { ...detail, ...body };
    mockTableDetails[key] = updated;
    return HttpResponse.json(updated);
  }),
  http.patch('/api/tables/:schema/:tableName/columns/:columnName', async ({ params, request }) => {
    const key = `${params.schema}.${params.tableName}`;
    const detail = mockTableDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as Record<string, unknown>;
    const col = detail.columns.find((c) => c.name === params.columnName);
    if (!col) return new HttpResponse(null, { status: 404 });
    Object.assign(col, body);
    return HttpResponse.json(col);
  }),

  // Views
  http.get('/api/views', ({ request }) => {
    const url = new URL(request.url);
    const offset = Number(url.searchParams.get('offset') ?? '0');
    const limit = Number(url.searchParams.get('limit') ?? '50');
    const page = mockViewSummaries.slice(offset, offset + limit);
    return HttpResponse.json({
      items: page,
      totalCount: mockViewSummaries.length,
      offset,
      limit,
    });
  }),
  http.get('/api/views/:schema/:name', ({ params }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockViewDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json(detail);
  }),
  http.patch('/api/views/:schema/:name', async ({ params, request }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockViewDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as Record<string, unknown>;
    const updated = { ...detail, ...body };
    mockViewDetails[key] = updated;
    return HttpResponse.json(updated);
  }),
  http.patch('/api/views/:schema/:viewName/columns/:columnName', async ({ params, request }) => {
    const key = `${params.schema}.${params.viewName}`;
    const detail = mockViewDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as Record<string, unknown>;
    const col = detail.columns.find((c) => c.name === params.columnName);
    if (!col) return new HttpResponse(null, { status: 404 });
    Object.assign(col, body);
    return HttpResponse.json(col);
  }),

  // Stored Procedures
  http.get('/api/stored-procedures', ({ request }) => {
    const url = new URL(request.url);
    const offset = Number(url.searchParams.get('offset') ?? '0');
    const limit = Number(url.searchParams.get('limit') ?? '50');
    const page = mockStoredProcedureSummaries.slice(offset, offset + limit);
    return HttpResponse.json({
      items: page,
      totalCount: mockStoredProcedureSummaries.length,
      offset,
      limit,
    });
  }),
  http.get('/api/stored-procedures/:schema/:name', ({ params }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockStoredProcedureDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json(detail);
  }),
  http.patch('/api/stored-procedures/:schema/:name', async ({ params, request }) => {
    const key = `${params.schema}.${params.name}`;
    const detail = mockStoredProcedureDetails[key];
    if (!detail) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as Record<string, unknown>;
    const updated = { ...detail, ...body };
    mockStoredProcedureDetails[key] = updated;
    return HttpResponse.json(updated);
  }),
];
