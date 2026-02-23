namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Response DTO for project configuration information.
/// </summary>
public record ProjectInfoResponse(
    string ProjectPath,
    string ModelName,
    string ModelSource,
    string PersistenceStrategy,
    bool ModelLoaded
);
