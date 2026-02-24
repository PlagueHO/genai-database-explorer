/** Maps to: ProjectInfoResponse.cs */
export interface ProjectInfo {
  projectPath: string;
  modelName: string;
  modelSource: string;
  persistenceStrategy: string;
  modelLoaded: boolean;
}

/** Maps to: SemanticModelSummaryResponse.cs */
export interface SemanticModelSummary {
  name: string;
  source: string;
  description: string | null;
  tableCount: number;
  viewCount: number;
  storedProcedureCount: number;
}

/** Maps to: PaginatedResponse<T>.cs */
export interface PaginatedResponse<T> {
  items: readonly T[];
  totalCount: number;
  offset: number;
  limit: number;
}

/** Maps to: EntitySummaryResponse.cs */
export interface EntitySummary {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  notUsed: boolean;
}

/** Maps to: TableDetailResponse.cs */
export interface TableDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null;
  details: string | null;
  additionalInformation: string | null;
  notUsed: boolean;
  notUsedReason: string | null;
  columns: readonly Column[];
  indexes: readonly Index[];
}

/** Maps to: ViewDetailResponse.cs */
export interface ViewDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null;
  additionalInformation: string | null;
  definition: string;
  notUsed: boolean;
  notUsedReason: string | null;
  columns: readonly Column[];
}

/** Maps to: StoredProcedureDetailResponse.cs */
export interface StoredProcedureDetail {
  schema: string;
  name: string;
  description: string | null;
  semanticDescription: string | null;
  semanticDescriptionLastUpdate: string | null;
  additionalInformation: string | null;
  parameters: string | null;
  definition: string;
  notUsed: boolean;
  notUsedReason: string | null;
}

/** Maps to: ColumnResponse.cs */
export interface Column {
  name: string;
  type: string | null;
  description: string | null;
  isPrimaryKey: boolean;
  isNullable: boolean;
  isIdentity: boolean;
  isComputed: boolean;
  isXmlDocument: boolean;
  maxLength: number | null;
  precision: number | null;
  scale: number | null;
  referencedTable: string | null;
  referencedColumn: string | null;
}

/** Maps to: IndexResponse.cs */
export interface Index {
  name: string;
  type: string | null;
  columnName: string | null;
  isUnique: boolean;
  isPrimaryKey: boolean;
  isUniqueConstraint: boolean;
}

/** Request body for entity description updates */
export interface UpdateEntityDescriptionRequest {
  description?: string | null;
  semanticDescription?: string | null;
  notUsed?: boolean | null;
  notUsedReason?: string | null;
}

/** Request body for column description updates */
export interface UpdateColumnDescriptionRequest {
  description?: string | null;
  semanticDescription?: string | null;
}

/** RFC 9457 Problem Details */
export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}
