using GenAIDBExplorer.Core.Models.Project;
using Microsoft.KernelMemory;

namespace GenAIDBExplorer.Core.KernelMemory
{
    public interface IKernelMemoryFactory
    {
        Func<IServiceProvider, IKernelMemory> CreateKernelMemory(IProject project);
    }
}
