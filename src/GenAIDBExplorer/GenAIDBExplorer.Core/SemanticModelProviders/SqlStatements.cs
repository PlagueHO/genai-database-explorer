namespace GenAIDBExplorer.Core.SemanticModelProviders;

/// <summary>
/// Contains SQL statements used by the <see cref="SemanticModelProvider"/>.
/// </summary>
internal static class SqlStatements
{
    /// <summary>
    /// SQL query to describe tables, including schema name, table name, and table description.
    /// </summary>
    public const string DescribeTables = @"
SELECT 
    S.name AS SchemaName,
    O.name AS TableName,
    ep.value AS TableDesc
FROM 
    sys.tables O
JOIN 
    sys.schemas S ON O.schema_id = S.schema_id
LEFT JOIN 
    sys.extended_properties EP ON ep.major_id = O.object_id 
    AND ep.name = 'MS_DESCRIPTION' 
    AND ep.minor_id = 0
";

    /// <summary>
    /// SQL query to describe tables, including schema name, table name, and table description.
    /// </summary>
    public const string DescribeViews = @"
SELECT 
    S.name AS SchemaName,
    V.name AS ViewName,
    ep.value AS ViewDesc
FROM 
    sys.views V
JOIN 
    sys.schemas S ON V.schema_id = S.schema_id
LEFT JOIN 
    sys.extended_properties EP ON ep.major_id = V.object_id 
    AND ep.name = 'MS_DESCRIPTION' 
    AND ep.minor_id = 0
";

    /// <summary>
    /// SQL query to describe columns for a specified table.
    /// </summary>
    public const string DescribeTableColumns = @"
SELECT
    sch.name AS SchemaName,
    tab.name AS TableName,
    col.name AS ColumnName,
    ep.value AS ColumnDesc,
    base.name AS ColumnType,
    CAST(IIF(ic.column_id IS NULL, 0, 1) AS bit) IsPK,
    col.max_length AS MaxLength,
    col.precision AS Precision,
    col.scale AS Scale,
    col.is_nullable AS IsNullable,
    col.is_identity AS IsIdentity,
    col.is_computed AS IsComputed,
    col.is_xml_document AS IsXmlDocument
FROM 
    (
        select object_id, schema_id, name, CAST(0 as bit) IsView from sys.tables
        UNION ALL
        select object_id, schema_id, name, CAST(1 as bit) IsView from sys.views
    ) tab
INNER JOIN
    sys.objects obj ON obj.object_id = tab.object_id
INNER JOIN
    sys.schemas sch ON tab.schema_id = sch.schema_id
INNER JOIN
    sys.columns col ON col.object_id = tab.object_id
INNER JOIN
    sys.types t ON col.user_type_id = t.user_type_id
INNER JOIN
    sys.types base ON t.system_type_id = base.user_type_id
LEFT OUTER JOIN 
    sys.indexes pk ON tab.object_id = pk.object_id AND pk.is_primary_key = 1
LEFT OUTER JOIN
    sys.index_columns ic ON ic.object_id = pk.object_id AND ic.index_id = pk.index_id AND ic.column_id = col.column_id 
LEFT OUTER JOIN
    sys.extended_properties ep ON ep.major_id = col.object_id AND ep.minor_id = col.column_id and ep.name = 'MS_DESCRIPTION'
WHERE
    sch.name != 'sys'
    AND sch.name = @SchemaName
    AND tab.name = @TableName
ORDER BY
    SchemaName, TableName, IsPK DESC, ColumnName
";

    /// <summary>
    /// SQL query to describe columns for a specified table.
    /// </summary>
    public const string DescribeViewColumns = @"
SELECT
    sch.name AS SchemaName,
    vw.name AS ViewName,
    col.name AS ColumnName,
    ep.value AS ColumnDesc,
    base.name AS ColumnType,
    col.max_length AS MaxLength,
    col.precision AS Precision,
    col.scale AS Scale,
    col.is_nullable AS IsNullable,
    col.is_identity AS IsIdentity,
    col.is_computed AS IsComputed,
    col.is_xml_document AS IsXmlDocument
FROM 
    sys.views vw
INNER JOIN
    sys.objects obj ON obj.object_id = vw.object_id
INNER JOIN
    sys.schemas sch ON vw.schema_id = sch.schema_id
INNER JOIN
    sys.columns col ON col.object_id = vw.object_id
INNER JOIN
    sys.types t ON col.user_type_id = t.user_type_id
INNER JOIN
    sys.types base ON t.system_type_id = base.user_type_id
LEFT OUTER JOIN
    sys.extended_properties ep ON ep.major_id = col.object_id AND ep.minor_id = col.column_id and ep.name = 'MS_DESCRIPTION'
WHERE
    sch.name != 'sys'
    AND sch.name = @SchemaName
    AND vw.name = @ViewName
ORDER BY
    SchemaName, ViewName, ColumnName
";

    public const string DescribeViewDefinition = @"
SELECT 
    sm.definition
FROM 
    sys.sql_modules AS sm
JOIN 
    sys.objects AS o ON sm.object_id = o.object_id
JOIN 
    sys.schemas AS s ON o.schema_id = s.schema_id
WHERE 
    o.type = 'V' AND 
    o.name = @ViewName AND 
    s.name = @SchemaName
";

    public const string DescribeIndexes = @"
SELECT
    sch.name AS SchemaName,
    tbl.name AS TableName,
    idx.name AS IndexName,
    idx.type_desc AS IndexType,
    col.name AS ColumnName,
    ic.is_included_column AS IsIncludedColumn,
    idx.is_unique AS IsUnique,
    idx.is_primary_key AS IsPrimaryKey,
    idx.is_unique_constraint AS IsUniqueConstraint
FROM
    sys.indexes idx
INNER JOIN
    sys.index_columns ic ON idx.object_id = ic.object_id AND idx.index_id = ic.index_id
INNER JOIN
    sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id
INNER JOIN
    sys.tables tbl ON idx.object_id = tbl.object_id
INNER JOIN
    sys.schemas sch ON tbl.schema_id = sch.schema_id
WHERE
    sch.name = @SchemaName
    AND tbl.name = @TableName
ORDER BY
    idx.name,
    ic.key_ordinal;
";

    public const string DescribeReferences = @"
SELECT
    obj.name AS KeyName,
    sch.name AS SchemaName,
    parentTab.name AS TableName,
    parentCol.name AS ColumnName,
    refTable.name AS ReferencedTableName,
    refCol.name AS ReferencedColumnName
FROM
	sys.foreign_key_columns fkc
INNER JOIN
	sys.objects obj ON obj.object_id = fkc.constraint_object_id
INNER JOIN
	sys.tables parentTab ON parentTab.object_id = fkc.parent_object_id
INNER JOIN
	sys.schemas sch ON parentTab.schema_id = sch.schema_id
INNER JOIN
	sys.columns parentCol ON parentCol.column_id = parent_column_id AND parentCol.object_id = parentTab.object_id
INNER JOIN
	sys.tables refTable ON refTable.object_id = fkc.referenced_object_id
INNER JOIN
	sys.columns refCol ON refCol.column_id = referenced_column_id AND refCol.object_id = refTable.object_id
WHERE
    sch.name = @SchemaName
    AND parentTab.name = @TableName
";

    public const string DescribeStoredProcedures = @"
SELECT
    schema_name(obj.schema_id) as schemaName,
    obj.name as procedureName,
    case type
        when 'P' then 'SQL Stored Procedure'
        when 'X' then 'Extended stored procedure'
    end as type,
    substring(par.parameters, 0, len(par.parameters)) as parameters,
    mod.definition as definition
FROM
    sys.objects obj JOIN sys.sql_modules mod ON mod.object_id = obj.object_id
    CROSS APPLY (
        SELECT
            p.name + ' ' + TYPE_NAME(p.user_type_id) + ', ' 
        FROM
            sys.parameters p
        WHERE
            p.object_id = obj.object_id
            AND p.parameter_id != 0 
        FOR XML path ('') ) par (parameters)
WHERE
    obj.type in ('P', 'X')
";

    public const string GetSampleTableData = @"
SELECT
    *
FROM (
    SELECT
        *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum
    FROM
        @EntityName
    ) AS RowConstrainedResult
WHERE
    RowNum <= @NumberOfRecords
ORDER BY
    RowNum
";

    public const string GetSampleTableDataRandom = @"
SELECT
    TOP (@NumberOfRecords) *
FROM
    @EntityName
ORDER BY
    NEWID()
";

    public const string GetSampleViewData = @"
SELECT
    *
FROM (
    SELECT
        *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum
    FROM
        @EntityName
    ) AS RowConstrainedResult
WHERE
    RowNum <= @NumberOfRecords
ORDER BY
    RowNum"";
";

    public const string GetSampleViewDataRandom = @"
SELECT
    TOP (@NumberOfRecords) *
FROM
    @EntityName
ORDER BY
    NEWID()
";

}
