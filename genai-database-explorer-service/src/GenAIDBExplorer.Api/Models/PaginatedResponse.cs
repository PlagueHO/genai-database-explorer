namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Generic paginated response wrapper for list endpoints.
/// </summary>
public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Offset,
    int Limit
);
