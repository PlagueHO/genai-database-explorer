using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using GenAIDBExplorer.Models.Project;

namespace GenAIDBExplorer.AI.SemanticKernel;

public class SemanticKernelFactory : ISemanticKernelFactory
{
    /// <summary>
    /// Factory method for <see cref="IServiceCollection"/>
    /// </summary>
    public Func<IServiceProvider, Kernel> CreateSemanticKernel(IProject project)
    {
        return CreateKernel;

        Kernel CreateKernel(IServiceProvider provider)
        {
            var builder = Kernel.CreateBuilder();

            builder.Services.AddLogging();

            return (Kernel)builder.Build();
        }
    }
}