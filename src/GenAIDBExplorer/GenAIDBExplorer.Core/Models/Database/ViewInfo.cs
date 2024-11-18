namespace GenAIDBExplorer.Core.Models.Database;

// Represents a view in the database
public record ViewInfo(
    string SchemaName,
    string ViewName
);
