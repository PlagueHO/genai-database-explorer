namespace GenAIDBExplorer.Core.Models.Database;

// Represents a stored procedure in the database
public record StoredProcedureInfo(
    string SchemaName,
    string ProcedureName,
    string ProcedureType,
    string? Parameters,
    string Definition
);