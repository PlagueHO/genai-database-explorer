using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Api.Models;

/// <summary>
/// Extension methods for mapping semantic model entities to API response DTOs.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a collection of <see cref="SemanticModelColumn"/> to a list of <see cref="ColumnResponse"/>.
    /// </summary>
    public static List<ColumnResponse> ToColumnResponses(this IEnumerable<SemanticModelColumn> columns) =>
        columns.Select(c => new ColumnResponse(
            c.Name, c.Type, c.Description, c.IsPrimaryKey, c.IsNullable,
            c.IsIdentity, c.IsComputed, c.IsXmlDocument, c.MaxLength,
            c.Precision, c.Scale, c.ReferencedTable, c.ReferencedColumn)).ToList();

    /// <summary>
    /// Maps a collection of <see cref="SemanticModelIndex"/> to a list of <see cref="IndexResponse"/>.
    /// </summary>
    public static List<IndexResponse> ToIndexResponses(this IEnumerable<SemanticModelIndex> indexes) =>
        indexes.Select(i => new IndexResponse(
            i.Name, i.Type, i.ColumnName,
            i.IsUnique, i.IsPrimaryKey, i.IsUniqueConstraint)).ToList();
}
