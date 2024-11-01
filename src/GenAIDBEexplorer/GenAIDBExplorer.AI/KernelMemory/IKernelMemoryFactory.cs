using GenAIDBExplorer.Models.Project;
using Microsoft.KernelMemory;
using System;

namespace GenAIDBExplorer.AI.KernelMemory
{
    public interface IKernelMemoryFactory
    {
        Func<IServiceProvider, IKernelMemory> CreateKernelMemory(IProject project);
    }
}
