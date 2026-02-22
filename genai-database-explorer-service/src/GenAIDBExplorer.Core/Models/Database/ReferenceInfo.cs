namespace GenAIDBExplorer.Core.Models.Database;

// Represents a reference between two tables
public record ReferenceInfo(
    string SchemaName,
    string TableName,
    string ColumnName,
    string ReferencedTableName,
    string ReferencedColumnName
);
