using GenAIDBExplorer.Models.Project;
using Microsoft.SemanticKernel;
using System;

namespace GenAIDBExplorer.AI.SemanticKernel
{
    public interface ISemanticKernelFactory
    {
        Kernel CreateSemanticKernel();
    }
}

