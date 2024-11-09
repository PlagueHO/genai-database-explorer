using GenAIDBExplorer.Models.SemanticModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Data.SemanticModelProviders;

public interface ISemanticModelProvider
{
    Task<SemanticModel> BuildSemanticModelAsync();
}