using FluentAssertions;
using Normaize.Core.Interfaces;
using Xunit;

namespace Normaize.Tests.Services;

public class IConfigurationValidationServiceTests
{
    [Fact]
    public void IConfigurationValidationService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IConfigurationValidationService);

        // Assert
        interfaceType.GetMethod("ValidateConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateDatabaseConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateSecurityConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateStorageConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateCachingConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ValidatePerformanceConfiguration", [typeof(CancellationToken)])!.Should().NotBeNull();
    }

    [Fact]
    public void IConfigurationValidationService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IConfigurationValidationService);

        // Assert
        interfaceType.GetMethod("ValidateConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
        interfaceType.GetMethod("ValidateDatabaseConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
        interfaceType.GetMethod("ValidateSecurityConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
        interfaceType.GetMethod("ValidateStorageConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
        interfaceType.GetMethod("ValidateCachingConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
        interfaceType.GetMethod("ValidatePerformanceConfiguration", [typeof(CancellationToken)])!.ReturnType.Should().Be<ConfigurationValidationResult>();
    }

    [Fact]
    public void IConfigurationValidationService_MethodsShouldHaveCancellationTokenParameter()
    {
        // Arrange
        var interfaceType = typeof(IConfigurationValidationService);
        var methodNames = new[]
        {
            "ValidateConfiguration",
            "ValidateDatabaseConfiguration",
            "ValidateSecurityConfiguration",
            "ValidateStorageConfiguration",
            "ValidateCachingConfiguration",
            "ValidatePerformanceConfiguration"
        };
        foreach (var name in methodNames)
        {
            var method = interfaceType.GetMethod(name, [typeof(CancellationToken)]);
            method.Should().NotBeNull();
            method!.GetParameters().Should().HaveCount(1);
            method.GetParameters()[0].ParameterType.Should().Be<CancellationToken>();
        }
    }
} 