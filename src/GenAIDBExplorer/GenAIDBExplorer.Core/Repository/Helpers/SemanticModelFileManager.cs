using System.IO;
using System.Text;
using System.Text.Json;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Repository.Security;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Repository.Helpers;

/// <summary>
/// Manages semantic model file operations.
/// </summary>
internal class SemanticModelFileManager
{
    private readonly ISecureJsonSerializer _secureJsonSerializer;
    private readonly ILogger _logger;

    public SemanticModelFileManager(ISecureJsonSerializer secureJsonSerializer, ILogger logger)
    {
        _secureJsonSerializer = secureJsonSerializer ?? throw new ArgumentNullException(nameof(secureJsonSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Saves the semantic model to the specified directory.
    /// </summary>
    public async Task SaveSemanticModelAsync(SemanticModel semanticModel, DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);
        ArgumentNullException.ThrowIfNull(directory);

        var semanticModelJsonPath = Path.Combine(directory.FullName, LocalDiskPersistenceConstants.SemanticModelFileName);
        var modelJsonOptions = CreateSemanticModelJsonOptions();

        try
        {
            var secureModelJson = await _secureJsonSerializer.SerializeAsync(semanticModel, modelJsonOptions);
            await File.WriteAllTextAsync(semanticModelJsonPath, secureModelJson, Encoding.UTF8);

            _logger.LogDebug("Successfully saved semantic model to: {FilePath}", semanticModelJsonPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save semantic model to: {FilePath}", semanticModelJsonPath);
            throw;
        }
    }

    /// <summary>
    /// Loads the semantic model from the specified directory.
    /// </summary>
    public async Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        var semanticModelFile = Path.Combine(directory.FullName, LocalDiskPersistenceConstants.SemanticModelFileName);
        if (!File.Exists(semanticModelFile))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelFile);
        }

        try
        {
            var json = await File.ReadAllTextAsync(semanticModelFile, Encoding.UTF8);

            // Parse and deserialize without applying input sanitizer to the entire JSON payload
            // to avoid false positives on legitimate model content.
            using (var _ = JsonDocument.Parse(json)) { }

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var semanticModel = JsonSerializer.Deserialize<SemanticModel>(json, jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");

            _logger.LogDebug("Successfully loaded semantic model from: {FilePath}", semanticModelFile);
            return semanticModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load semantic model from: {FilePath}", semanticModelFile);
            throw;
        }
    }

    /// <summary>
    /// Checks if a semantic model exists at the specified path.
    /// </summary>
    public bool Exists(DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (!directory.Exists)
            return false;

        var semanticModelFile = Path.Combine(directory.FullName, LocalDiskPersistenceConstants.SemanticModelFileName);
        return File.Exists(semanticModelFile);
    }

    private static JsonSerializerOptions CreateSemanticModelJsonOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelTableJsonConverter());
        options.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelViewJsonConverter());
        options.Converters.Add(new Models.SemanticModel.JsonConverters.SemanticModelStoredProcedureJsonConverter());
        return options;
    }
}