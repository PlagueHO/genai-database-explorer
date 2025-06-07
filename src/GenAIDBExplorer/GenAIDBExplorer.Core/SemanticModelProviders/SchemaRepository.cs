using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.SemanticModel.UsageStrategy;
using Microsoft.Extensions.Logging;
using System.Resources;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public sealed class SchemaRepository(
    ISqlQueryExecutor sqlQueryExecutor,
    IProject project,
    ILogger<SchemaRepository> logger
) : ISchemaRepository
{
    private readonly ISqlQueryExecutor _sqlQueryExecutor = sqlQueryExecutor;
    private readonly IProject _project = project;
    private readonly ILogger<SchemaRepository> _logger = logger;
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SchemaRepository).Assembly);

    /// <summary>
    /// Retrieves a list of tables from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter tables by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and table name, and the value is the table info.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    public async Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null)
    {
        var tables = new Dictionary<string, TableInfo>();
        try
        {
            var query = SqlStatements.DescribeTables;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " WHERE S.name = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var tableName = reader.GetString(1);
                tables.Add($"{schemaName}.{tableName}", new TableInfo(schemaName, tableName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage}", _resourceManagerErrorMessages.GetString("ErrorGettingTablesFromDatabase"));
            throw;
        }

        return tables;
    }

    /// <summary>
    /// Retrieves a list of views from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter view by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and table name, and the value is the table name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    public async Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null)
    {
        var views = new Dictionary<string, ViewInfo>();

        try
        {
            var query = SqlStatements.DescribeViews;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " WHERE S.name = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var viewName = reader.GetString(1);
                views.Add($"{schemaName}.{viewName}", new ViewInfo(schemaName, viewName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage}", _resourceManagerErrorMessages.GetString("ErrorGettingViewsFromDatabase"));
            throw;
        }

        return views;
    }

    /// <summary>
    /// Retrieves a list of stored procedures from the database, optionally filtered by schema.
    /// </summary>
    /// <param name="schema">The schema to filter stored procedures by. If null, all schemas are included.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the key is the schema and procedure name, and the value is the procedure name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    public async Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null)
    {
        var storedProcedures = new Dictionary<string, StoredProcedureInfo>();

        try
        {
            var query = SqlStatements.DescribeStoredProcedures;
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(schema))
            {
                query += " AND schema_name(obj.schema_id) = @Schema";
                parameters.Add("@Schema", schema);
            }

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(0);
                var procedureName = reader.GetString(1);
                var procedureType = reader.GetString(2);
                var parametersList = reader.IsDBNull(3) ? null : reader.GetString(3);
                var definition = reader.GetString(4);
                storedProcedures.Add($"{schemaName}.{procedureName}", new StoredProcedureInfo(schemaName, procedureName, procedureType, parametersList, definition));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage}", _resourceManagerErrorMessages.GetString("ErrorGettingStoredProceduresFromDatabase"));
            throw;
        }

        return storedProcedures;
    }

    /// <summary>
    /// Retrieves the definition of the specified view.
    /// </summary>
    /// <param name="view">The view info for the view to retrieve the definition for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the definition of the view.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the database connection is not open.</exception>
    /// <exception cref="SqlException">Thrown when there is an error executing the SQL query.</exception>
    public async Task<string> GetViewDefinitionAsync(ViewInfo view)
    {
        try
        {
            var query = SqlStatements.DescribeViewDefinition;
            var parameters = new Dictionary<string, object> {
            { "@SchemaName", view.SchemaName },
            { "@ViewName", view.ViewName }
        };

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                return reader.GetString(0);
            }
            else
            {
                throw new InvalidOperationException($"View definition for {view.SchemaName}.{view.ViewName} not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{ViewName}]", _resourceManagerErrorMessages.GetString("ErrorGettingViewDefinitionFromDatabase"), view.SchemaName, view.ViewName);
            throw;
        }
    }

    /// <summary>
    /// Creates a Semantic Model Table for the specified table by querying the columns and keys.
    /// </summary>
    /// <param name="table">The table info for the table to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    public async Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table)
    {
        var semanticModelTable = new SemanticModelTable(table.SchemaName, table.TableName);

        // Apply usage settings based on regex patterns from DatabaseSettings
        var tableStrategy = new RegexEntityUsageStrategy<SemanticModelTable>();
        var tableRegexPatterns = _project.Settings.Database.NotUsedTables;
        semanticModelTable.ApplyUsageSettings(tableStrategy, tableRegexPatterns);

        // Get the columns for the table
        var columns = await GetColumnsForTableAsync(table).ConfigureAwait(false);

        // Get the references for the table
        var references = await GetReferencesForTableAsync(table).ConfigureAwait(false);

        // Match references with columns and set the referenced properties
        foreach (var reference in references)
        {
            var column = columns.FirstOrDefault(c => c.Name == reference.ColumnName);
            if (column != null)
            {
                column.ReferencedTable = reference.ReferencedTableName;
                column.ReferencedColumn = reference.ReferencedColumnName;
            }
        }

        semanticModelTable.Columns.AddRange(columns);

        // Get the indexes for the table
        var indexes = await GetIndexesForTableAsync(table).ConfigureAwait(false);
        semanticModelTable.Indexes.AddRange(indexes);

        return semanticModelTable;
    }

    /// <summary>
    /// Creates a Semantic Model View for the specified view by querying the columns and keys.
    /// </summary>
    /// <param name="view">The view info for the view to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelTable"/>.</returns>
    public async Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view)
    {
        var semanticModelView = new SemanticModelView(view.SchemaName, view.ViewName);

        // Apply usage settings based on regex patterns from DatabaseSettings
        var viewStrategy = new RegexEntityUsageStrategy<SemanticModelView>();
        var viewRegexPatterns = _project.Settings.Database.NotUsedViews;
        semanticModelView.ApplyUsageSettings(viewStrategy, viewRegexPatterns);

        // Get the columns for the view
        var columns = await GetColumnsForViewAsync(view).ConfigureAwait(false);
        semanticModelView.Columns.AddRange(columns);

        // Add the view definition
        semanticModelView.Definition = await GetViewDefinitionAsync(view).ConfigureAwait(false);

        return semanticModelView;
    }

    /// <summary>
    /// Creates a Semantic Model Stored Procedure for the specified stored procedure info.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure info to create a semantic model for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the created <see cref="SemanticModelStoredProcedure"/>.</returns>
    public Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure)
    {
        var semanticModelStoredProcedure = new SemanticModelStoredProcedure(
            storedProcedure.SchemaName,
            storedProcedure.ProcedureName,
            storedProcedure.Definition,
            storedProcedure.Parameters
        );

        // Apply usage settings based on regex patterns from DatabaseSettings
        var storedProcedureStrategy = new RegexEntityUsageStrategy<SemanticModelStoredProcedure>();
        var storedProcedureRegexPatterns = _project.Settings.Database.NotUsedStoredProcedures;
        semanticModelStoredProcedure.ApplyUsageSettings(storedProcedureStrategy, storedProcedureRegexPatterns);

        return Task.FromResult(semanticModelStoredProcedure);
    }

    /// <summary>
    /// Retrieves the columns for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified table.</returns>
    public async Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table)
    {
        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = SqlStatements.DescribeTableColumns;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // get the contents of column schemaName in the reader into a var 
                var columnName = reader.GetString(2);
                var column = new SemanticModelColumn(table.SchemaName, columnName)
                {
                    Type = reader.GetString(4),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsPrimaryKey = reader.GetBoolean(5),
                    MaxLength = reader.GetInt16(6),
                    Precision = reader.GetByte(7),
                    Scale = reader.GetByte(8),
                    IsNullable = reader.GetBoolean(9),
                    IsIdentity = reader.GetBoolean(10),
                    IsComputed = reader.GetBoolean(11),
                    IsXmlDocument = reader.GetBoolean(12)
                };
                semanticModelColumns.Add(column);

                // Apply usage settings based on regex patterns from DatabaseSettings
                var strategy = new RegexColumnUsageStrategy();
                var regexPatterns = _project.Settings.Database.NotUsedColumns;
                semanticModelColumns.ApplyUsageSettings(strategy, regexPatterns);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{TableName}]", _resourceManagerErrorMessages.GetString("ErrorGettingColumnsForTable"), table.SchemaName, table.TableName);
            throw;

        }

        return semanticModelColumns;
    }

    /// <summary>
    /// Retrieves the references for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract references for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="ReferenceInfo"/> for the specified table.</returns>
    public async Task<List<ReferenceInfo>> GetReferencesForTableAsync(TableInfo table)
    {
        var references = new List<ReferenceInfo>();

        try
        {
            var query = SqlStatements.DescribeReferences;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var schemaName = reader.GetString(1);
                var tableName = reader.GetString(2);
                var columnName = reader.GetString(3);
                var referencedTableName = reader.GetString(4);
                var referencedColumnName = reader.GetString(5);

                var reference = new ReferenceInfo(
                    schemaName,
                    tableName,
                    columnName,
                    referencedTableName,
                    referencedColumnName
                );

                references.Add(reference);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{TableName}]", _resourceManagerErrorMessages.GetString("ErrorGettingReferencesForTable"), table.SchemaName, table.TableName);
            throw;
        }

        return references;
    }

    /// <summary>
    /// Retrieves the columns for the specified view.
    /// </summary>
    /// <param name="view">The view info for the view to extract columns for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelColumn"/> for the specified view.</returns>
    public async Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view)
    {
        var semanticModelColumns = new List<SemanticModelColumn>();

        try
        {
            var query = SqlStatements.DescribeViewColumns;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", view.SchemaName },
                { "@ViewName", view.ViewName }
            };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var columnName = reader.GetString(2);
                var column = new SemanticModelColumn(view.SchemaName, columnName)
                {
                    Type = reader.GetString(4),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    MaxLength = reader.GetInt16(5),
                    Precision = reader.GetByte(6),
                    Scale = reader.GetByte(7),
                    IsNullable = reader.GetBoolean(8),
                    IsIdentity = reader.GetBoolean(9),
                    IsComputed = reader.GetBoolean(10),
                    IsXmlDocument = reader.GetBoolean(11)
                };
                semanticModelColumns.Add(column);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{ViewName}]", _resourceManagerErrorMessages.GetString("ErrorGettingColumnsForView"), view.SchemaName, view.ViewName);
            throw;
        }

        return semanticModelColumns;
    }

    /// <summary>
    /// Retrieves the indexes for the specified table.
    /// </summary>
    /// <param name="table">The table info for the table to extract indexes for.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of <see cref="SemanticModelIndex"/> for the specified table.</returns>
    public async Task<List<SemanticModelIndex>> GetIndexesForTableAsync(TableInfo table)
    {
        var indexes = new List<SemanticModelIndex>();

        try
        {
            var query = SqlStatements.DescribeIndexes;
            var parameters = new Dictionary<string, object> {
                { "@SchemaName", table.SchemaName },
                { "@TableName", table.TableName }
            };
            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var indexName = reader.GetString(2);
                var indexType = reader.GetString(3);
                var columnName = reader.GetString(4);
                var isIncludedColumn = reader.GetBoolean(5);
                var isUnique = reader.GetBoolean(6);
                var isPrimaryKey = reader.GetBoolean(7);
                var isUniqueConstraint = reader.GetBoolean(8);

                var index = indexes.FirstOrDefault(i => i.Name == indexName);
                if (index == null)
                {
                    index = new SemanticModelIndex(table.SchemaName, indexName)
                    {
                        Type = indexType,
                        ColumnName = columnName,
                        IsUnique = isUnique,
                        IsPrimaryKey = isPrimaryKey,
                        IsUniqueConstraint = isUniqueConstraint,
                    };
                    indexes.Add(index);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{TableName}]", _resourceManagerErrorMessages.GetString("ErrorGettingIndexesForTable"), table.SchemaName, table.TableName);
            throw;
        }

        return indexes;
    }

    /// <summary>
    /// Retrieves sample data for the specified table by selecting the most recent records or a random sample.
    /// </summary>
    /// <param name="tableInfo">The table info for the table to retrieve sample data for.</param>
    /// <param name="numberOfRecords">The number of records to retrieve.</param>
    /// <param name="selectRandom">Whether to select a random sample of records.</param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(
        TableInfo tableInfo,
        int numberOfRecords = 5,
        bool selectRandom = false
    )
    {
        try
        {
            var query = (selectRandom ? SqlStatements.GetSampleTableDataRandom : SqlStatements.GetSampleTableData)
                .Replace("@EntityName", $"[{tableInfo.SchemaName}].[{tableInfo.TableName}]");

            var parameters = new Dictionary<string, object>
            {
                { "@NumberOfRecords", numberOfRecords }
            };

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            var rows = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = new Dictionary<string, object>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{TableName}]", _resourceManagerErrorMessages.GetString("ErrorGettingSampleDataForTable"), tableInfo.SchemaName, tableInfo.TableName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves sample data for the specified view by selecting the most recent records or a random sample.
    /// </summary>
    /// <param name="viewInfo">The view info for the view to retrieve sample data for.</param>
    /// <param name="numberOfRecords">The number of records to retrieve.</param>
    /// <param name="selectRandom">Whether to select a random sample of records.</param>
    public async Task<List<Dictionary<string, object>>> GetSampleViewDataAsync(
        ViewInfo viewInfo,
        int numberOfRecords = 5,
        bool selectRandom = false
    )
    {
        try
        {
            var query = (selectRandom ? SqlStatements.GetSampleViewDataRandom : SqlStatements.GetSampleViewData)
                .Replace("@EntityName", $"[{viewInfo.SchemaName}].[{viewInfo.ViewName}]");
            var parameters = new Dictionary<string, object>
            {
                { "@NumberOfRecords", numberOfRecords },
            };

            using var reader = await _sqlQueryExecutor.ExecuteReaderAsync(query, parameters).ConfigureAwait(false);

            var rows = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = new Dictionary<string, object>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage} [{SchemaName}].[{ViewName}]", _resourceManagerErrorMessages.GetString("ErrorGettingSampleDataForView"), viewInfo.SchemaName, viewInfo.ViewName);
            throw;
        }
    }
}
