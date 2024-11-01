using GenAIDBExplorer.Models.Project;
using Microsoft.SemanticKernel;
using System;

namespace GenAIDBExplorer.AI.SemanticKernel
{
    public interface ISemanticKernelFactory
    {
        Func<IServiceProvider, Kernel> CreateSemanticKernel(IProject project);
    }
}

