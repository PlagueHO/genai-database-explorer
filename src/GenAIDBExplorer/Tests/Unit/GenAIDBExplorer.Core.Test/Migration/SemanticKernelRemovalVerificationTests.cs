using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Migration;

/// <summary>
/// Verification tests to ensure all Semantic Kernel references have been removed
/// except for Microsoft.SemanticKernel.Connectors.InMemory in vector store files.
/// </summary>
[TestClass]
public class SemanticKernelRemovalVerificationTests
{
    private static readonly string[] AllowedFiles =
    [
        "SkInMemoryVectorIndexWriter.cs",
        "SkInMemoryVectorSearchService.cs",
        "IVectorStoreAdapter.cs",
        "InMemoryVectorStoreAdapter.cs",
        "InMemoryE2ETests.cs",
        "HostBuilderExtensions.cs", // InMemoryVectorStore registration
        "SemanticKernelRemovalVerificationTests.cs" // This test file itself
    ];

    private static string GetCoreSourceRoot()
    {
        // Navigate from test output directory up to the solution root
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "GenAIDBExplorer.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? throw new InvalidOperationException("Could not find solution root");
    }

    [TestMethod]
    public void NoCsFiles_ShouldContain_UsingMicrosoftSemanticKernel_ExceptInMemoryConnector()
    {
        // Arrange
        var root = GetCoreSourceRoot();
        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Act
        var violations = csFiles
            .Where(f => !AllowedFiles.Any(a => f.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains("using Microsoft.SemanticKernel")
                    && !content.Contains("using Microsoft.SemanticKernel.Connectors.InMemory");
            })
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();

        // Assert
        violations.Should().BeEmpty(
            "no .cs files should reference Microsoft.SemanticKernel (except Connectors.InMemory in allowed files)");
    }

    [TestMethod]
    public void NoCsFiles_ShouldContain_KernelArguments()
    {
        // Arrange
        var root = GetCoreSourceRoot();
        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Act
        var violations = csFiles
            .Where(f => !AllowedFiles.Any(a => f.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
            .Where(f => File.ReadAllText(f).Contains("KernelArguments"))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();

        // Assert
        violations.Should().BeEmpty("no .cs files should reference KernelArguments");
    }

    [TestMethod]
    public void NoCsFiles_ShouldContain_PromptExecutionSettings()
    {
        // Arrange
        var root = GetCoreSourceRoot();
        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Act
        var violations = csFiles
            .Where(f => !AllowedFiles.Any(a => f.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
            .Where(f => File.ReadAllText(f).Contains("PromptExecutionSettings"))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();

        // Assert
        violations.Should().BeEmpty("no .cs files should reference PromptExecutionSettings");
    }

    [TestMethod]
    public void NoCsFiles_ShouldContain_SKEXP_PragmaWarnings()
    {
        // Arrange
        var root = GetCoreSourceRoot();
        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        // Act
        var violations = csFiles
            .Where(f => !AllowedFiles.Any(a => f.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
            .Where(f => File.ReadAllText(f).Contains("SKEXP"))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();

        // Assert
        violations.Should().BeEmpty("no .cs files should contain #pragma warning disable SKEXP* suppressions");
    }
}
