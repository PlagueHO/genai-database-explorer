namespace GenAIDBExplorer.Core.DataDictionary;

// Represents a table column extracted from a data dictionary file
internal record ColumnDataDictionary(
    string ColumnName,
    string Type,
    int? Size,
    string Description,
    Boolean NotUsed
);
