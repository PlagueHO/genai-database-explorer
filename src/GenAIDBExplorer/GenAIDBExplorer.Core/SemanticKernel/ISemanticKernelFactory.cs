using Microsoft.SemanticKernel;

namespace GenAIDBExplorer.Core.SemanticKernel
{
    public interface ISemanticKernelFactory
    {
        Kernel CreateSemanticKernel();
    }
}
