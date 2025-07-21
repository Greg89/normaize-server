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
        interfaceType.GetMethod("ValidateConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateDatabaseConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateSecurityConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateStorageConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ValidateCachingConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ValidatePerformanceConfiguration", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
    }

    [Fact]
    public void IConfigurationValidationService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IConfigurationValidationService);

        // Assert
        interfaceType.GetMethod("ValidateConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
        interfaceType.GetMethod("ValidateDatabaseConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
        interfaceType.GetMethod("ValidateSecurityConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
        interfaceType.GetMethod("ValidateStorageConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
        interfaceType.GetMethod("ValidateCachingConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
        interfaceType.GetMethod("ValidatePerformanceConfiguration", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(ConfigurationValidationResult));
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
            var method = interfaceType.GetMethod(name, new[] { typeof(CancellationToken) });
            method.Should().NotBeNull();
            method!.GetParameters().Should().HaveCount(1);
            method.GetParameters()[0].ParameterType.Should().Be(typeof(CancellationToken));
        }
    }
} 