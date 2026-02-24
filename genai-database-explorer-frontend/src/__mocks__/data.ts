import type {
  ProjectInfo,
  SemanticModelSummary,
  EntitySummary,
  TableDetail,
  ViewDetail,
  StoredProcedureDetail,
} from '../types/api';

export const mockProject: ProjectInfo = {
  projectPath: '/mock/adventureworks',
  modelName: 'AdventureWorksLT',
  modelSource: 'SQL Server',
  persistenceStrategy: 'LocalDisk',
  modelLoaded: true,
};

export const mockModelSummary: SemanticModelSummary = {
  name: 'AdventureWorksLT',
  source: 'SQL Server',
  description: 'A lightweight sample database for demos and prototyping.',
  tableCount: 3,
  viewCount: 2,
  storedProcedureCount: 1,
};

export const mockTableSummaries: EntitySummary[] = [
  {
    schema: 'SalesLT',
    name: 'Product',
    description: 'Products for sale or in development.',
    semanticDescription: 'Contains information about bike products and accessories.',
    notUsed: false,
  },
  {
    schema: 'SalesLT',
    name: 'Customer',
    description: 'Customer records.',
    semanticDescription: 'Contains customer demographics and contact information.',
    notUsed: false,
  },
  {
    schema: 'SalesLT',
    name: 'SalesOrderHeader',
    description: 'General sales order information.',
    semanticDescription: 'Contains order-level data including dates, status, and totals.',
    notUsed: false,
  },
];

export const mockViewSummaries: EntitySummary[] = [
  {
    schema: 'SalesLT',
    name: 'vProductAndDescription',
    description: 'Product names with descriptions by culture.',
    semanticDescription: 'Joins Product and ProductDescription for localized display.',
    notUsed: false,
  },
  {
    schema: 'SalesLT',
    name: 'vGetAllCategories',
    description: 'Hierarchical product categories.',
    semanticDescription: 'Recursive CTE that builds the full category tree.',
    notUsed: false,
  },
];

export const mockStoredProcedureSummaries: EntitySummary[] = [
  {
    schema: 'dbo',
    name: 'uspGetManagerEmployees',
    description: 'Returns the direct reports for a given manager.',
    semanticDescription: 'Hierarchical query for the org chart.',
    notUsed: false,
  },
];

export const mockTableDetails: Record<string, TableDetail> = {
  'SalesLT.Product': {
    schema: 'SalesLT',
    name: 'Product',
    description: 'Products for sale or in development.',
    semanticDescription: 'Contains information about bike products and accessories.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    details: null,
    additionalInformation: null,
    notUsed: false,
    notUsedReason: null,
    columns: [
      { name: 'ProductID', type: 'int', description: 'Primary key.', isPrimaryKey: true, isNullable: false, isIdentity: true, isComputed: false, isXmlDocument: false, maxLength: null, precision: 10, scale: 0, referencedTable: null, referencedColumn: null },
      { name: 'Name', type: 'nvarchar', description: 'Product name.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'ProductNumber', type: 'nvarchar', description: 'Unique product number.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 25, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'Color', type: 'nvarchar', description: 'Product color.', isPrimaryKey: false, isNullable: true, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 15, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'ListPrice', type: 'money', description: 'Selling price.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: null, precision: 19, scale: 4, referencedTable: null, referencedColumn: null },
    ],
    indexes: [
      { name: 'PK_Product_ProductID', type: 'CLUSTERED', columnName: 'ProductID', isUnique: true, isPrimaryKey: true, isUniqueConstraint: false },
      { name: 'AK_Product_Name', type: 'NONCLUSTERED', columnName: 'Name', isUnique: true, isPrimaryKey: false, isUniqueConstraint: true },
    ],
  },
  'SalesLT.Customer': {
    schema: 'SalesLT',
    name: 'Customer',
    description: 'Customer records.',
    semanticDescription: 'Contains customer demographics and contact information.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    details: null,
    additionalInformation: null,
    notUsed: false,
    notUsedReason: null,
    columns: [
      { name: 'CustomerID', type: 'int', description: 'Primary key.', isPrimaryKey: true, isNullable: false, isIdentity: true, isComputed: false, isXmlDocument: false, maxLength: null, precision: 10, scale: 0, referencedTable: null, referencedColumn: null },
      { name: 'FirstName', type: 'nvarchar', description: 'First name.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'LastName', type: 'nvarchar', description: 'Last name.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'EmailAddress', type: 'nvarchar', description: 'Email address.', isPrimaryKey: false, isNullable: true, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
    ],
    indexes: [
      { name: 'PK_Customer_CustomerID', type: 'CLUSTERED', columnName: 'CustomerID', isUnique: true, isPrimaryKey: true, isUniqueConstraint: false },
    ],
  },
  'SalesLT.SalesOrderHeader': {
    schema: 'SalesLT',
    name: 'SalesOrderHeader',
    description: 'General sales order information.',
    semanticDescription: 'Contains order-level data including dates, status, and totals.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    details: null,
    additionalInformation: null,
    notUsed: false,
    notUsedReason: null,
    columns: [
      { name: 'SalesOrderID', type: 'int', description: 'Primary key.', isPrimaryKey: true, isNullable: false, isIdentity: true, isComputed: false, isXmlDocument: false, maxLength: null, precision: 10, scale: 0, referencedTable: null, referencedColumn: null },
      { name: 'OrderDate', type: 'datetime', description: 'Date the order was placed.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: null, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'CustomerID', type: 'int', description: 'Customer who placed the order.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: null, precision: 10, scale: 0, referencedTable: 'SalesLT.Customer', referencedColumn: 'CustomerID' },
      { name: 'TotalDue', type: 'money', description: 'Total amount due.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: true, isXmlDocument: false, maxLength: null, precision: 19, scale: 4, referencedTable: null, referencedColumn: null },
    ],
    indexes: [
      { name: 'PK_SalesOrderHeader_SalesOrderID', type: 'CLUSTERED', columnName: 'SalesOrderID', isUnique: true, isPrimaryKey: true, isUniqueConstraint: false },
    ],
  },
};

export const mockViewDetails: Record<string, ViewDetail> = {
  'SalesLT.vProductAndDescription': {
    schema: 'SalesLT',
    name: 'vProductAndDescription',
    description: 'Product names with descriptions by culture.',
    semanticDescription: 'Joins Product and ProductDescription for localized display.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    additionalInformation: null,
    definition: 'SELECT p.ProductID, p.Name, pd.Description, pm.Culture FROM SalesLT.Product p ...',
    notUsed: false,
    notUsedReason: null,
    columns: [
      { name: 'ProductID', type: 'int', description: 'Product identifier.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: null, precision: 10, scale: 0, referencedTable: null, referencedColumn: null },
      { name: 'Name', type: 'nvarchar', description: 'Product name.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'Description', type: 'nvarchar', description: 'Localized description.', isPrimaryKey: false, isNullable: true, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 400, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'Culture', type: 'nchar', description: 'Language culture code.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 6, precision: null, scale: null, referencedTable: null, referencedColumn: null },
    ],
  },
  'SalesLT.vGetAllCategories': {
    schema: 'SalesLT',
    name: 'vGetAllCategories',
    description: 'Hierarchical product categories.',
    semanticDescription: 'Recursive CTE that builds the full category tree.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    additionalInformation: null,
    definition: 'WITH CategoryCTE AS (...) SELECT * FROM CategoryCTE',
    notUsed: false,
    notUsedReason: null,
    columns: [
      { name: 'ParentProductCategoryName', type: 'nvarchar', description: 'Parent category name.', isPrimaryKey: false, isNullable: true, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
      { name: 'ProductCategoryName', type: 'nvarchar', description: 'Category name.', isPrimaryKey: false, isNullable: false, isIdentity: false, isComputed: false, isXmlDocument: false, maxLength: 50, precision: null, scale: null, referencedTable: null, referencedColumn: null },
    ],
  },
};

export const mockStoredProcedureDetails: Record<string, StoredProcedureDetail> = {
  'dbo.uspGetManagerEmployees': {
    schema: 'dbo',
    name: 'uspGetManagerEmployees',
    description: 'Returns the direct reports for a given manager.',
    semanticDescription: 'Hierarchical query for the org chart.',
    semanticDescriptionLastUpdate: '2026-01-15T10:30:00Z',
    additionalInformation: null,
    parameters: '@ManagerID int',
    definition: 'CREATE PROCEDURE dbo.uspGetManagerEmployees @ManagerID int AS ...',
    notUsed: false,
    notUsedReason: null,
  },
};
