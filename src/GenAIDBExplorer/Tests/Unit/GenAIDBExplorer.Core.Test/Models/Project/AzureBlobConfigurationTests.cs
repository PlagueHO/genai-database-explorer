using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Test.Models.Project;

/// <summary>
/// Unit tests for <see cref="AzureBlobConfiguration"/> validation.
/// </summary>
[TestClass]
public class AzureBlobConfigurationTests
{
    [TestMethod]
    public void CustomerManagedKeyUrl_EmptyString_ShouldPassValidation()
    {
        // Arrange
        var config = new AzureBlobConfiguration
        {
            AccountEndpoint = "https://test.blob.core.windows.net",
            CustomerManagedKeyUrl = ""
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void CustomerManagedKeyUrl_NullValue_ShouldPassValidation()
    {
        // Arrange
        var config = new AzureBlobConfiguration
        {
            AccountEndpoint = "https://test.blob.core.windows.net",
            CustomerManagedKeyUrl = null
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void CustomerManagedKeyUrl_ValidUrl_ShouldPassValidation()
    {
        // Arrange
        var config = new AzureBlobConfiguration
        {
            AccountEndpoint = "https://test.blob.core.windows.net",
            CustomerManagedKeyUrl = "https://keyvault.vault.azure.net/keys/mykey/version"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void CustomerManagedKeyUrl_InvalidUrl_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureBlobConfiguration
        {
            AccountEndpoint = "https://test.blob.core.windows.net",
            CustomerManagedKeyUrl = "invalid-url"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().HaveCount(1);
        validationResults[0].ErrorMessage.Should().Contain("CustomerManagedKeyUrl");
        validationResults[0].ErrorMessage.Should().Contain("not a valid fully-qualified");
    }

    [TestMethod]
    public void ConditionalUrlAttribute_EmptyString_ShouldBeValid()
    {
        // Arrange
        var attribute = new ConditionalUrlAttribute();

        // Act
        var isValid = attribute.IsValid("");

        // Assert
        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void ConditionalUrlAttribute_NullValue_ShouldBeValid()
    {
        // Arrange
        var attribute = new ConditionalUrlAttribute();

        // Act
        var isValid = attribute.IsValid(null);

        // Assert
        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void ConditionalUrlAttribute_ValidUrl_ShouldBeValid()
    {
        // Arrange
        var attribute = new ConditionalUrlAttribute();

        // Act
        var isValid = attribute.IsValid("https://example.com");

        // Assert
        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void ConditionalUrlAttribute_InvalidUrl_ShouldBeInvalid()
    {
        // Arrange
        var attribute = new ConditionalUrlAttribute();

        // Act
        var isValid = attribute.IsValid("not-a-url");

        // Assert
        isValid.Should().BeFalse();
    }
}