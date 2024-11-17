using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

namespace GenAIDBExplorer.Core.KernelMemory
{
    public class KernelMemoryFactory : IKernelMemoryFactory
    {
        /// <summary>
        /// Factory method for <see cref="IServiceCollection"/>
        /// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public Func<IServiceProvider, IKernelMemory> CreateKernelMemory(IProject project)
        {
            return CreateKernel;

            IKernelMemory CreateKernel(IServiceProvider provider)
            {
                var kernelMemoryBuilder = new KernelMemoryBuilder();

                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    kernelMemoryBuilder.Services.AddSingleton(loggerFactory);
                }

                return kernelMemoryBuilder.Build();

            }
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}