namespace GenAIDBExplorer.Core.DataDictionary;

// Represents a table extracted from a data dictionary file
record TableDataDictionary(
    string SchemaName,
    string TableName,
    string Description,
    string Details,
    string AdditionalInformation,
    List<ColumnDataDictionary> Columns
);
