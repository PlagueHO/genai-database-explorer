namespace GenAIDBExplorer.Core.Models.Database;

// Represents a table in the database
public record TableInfo(
    string SchemaName,
    string TableName
);
