using GenAIDBExplorer.Models.SemanticModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public interface ISemanticModelProvider
{
    /// <summary>
    /// Creates a new empty semantic model, configured with the project information.
    /// </summary>
    /// <returns>Returns the empty configured <see cref="SemanticModel"/>.</returns>
    SemanticModel CreateSemanticModel();

    /// <summary>
    /// Builds the semantic model asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the built <see cref="SemanticModel"/>.</returns>
    Task<SemanticModel> BuildSemanticModelAsync();
}