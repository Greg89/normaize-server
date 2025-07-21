using FluentAssertions;
using Normaize.Core.Interfaces;
using Xunit;

namespace Normaize.Tests.Services;

public class StartupServiceTests
{
    [Fact]
    public void IStartupService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IStartupService);

        // Assert
        interfaceType.GetMethod("ConfigureStartupAsync", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("ShouldRunStartupChecks")!.Should().NotBeNull();
        interfaceType.GetMethod("ApplyMigrationsAsync", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
        interfaceType.GetMethod("PerformHealthChecksAsync", new[] { typeof(CancellationToken) })!.Should().NotBeNull();
    }

    [Fact]
    public void IStartupService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);

        // Assert
        interfaceType.GetMethod("ConfigureStartupAsync", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(Task));
        interfaceType.GetMethod("ShouldRunStartupChecks")!.ReturnType.Should().Be(typeof(bool));
        interfaceType.GetMethod("ApplyMigrationsAsync", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(Task));
        interfaceType.GetMethod("PerformHealthChecksAsync", new[] { typeof(CancellationToken) })!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void IStartupService_ShouldBePublic()
    {
        // Assert
        typeof(IStartupService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IStartupService_ShouldBeInterface()
    {
        // Assert
        typeof(IStartupService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IStartupService_ConfigureStartupAsync_ShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ConfigureStartupAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStartupService_ShouldRunStartupChecks_ShouldReturnBool()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ShouldRunStartupChecks");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void IStartupService_ApplyMigrationsAsync_ShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ApplyMigrationsAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStartupService_PerformHealthChecksAsync_ShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("PerformHealthChecksAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStartupService_ConfigureStartupAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ConfigureStartupAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void IStartupService_ApplyMigrationsAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ApplyMigrationsAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void IStartupService_PerformHealthChecksAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("PerformHealthChecksAsync", new[] { typeof(CancellationToken) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void IStartupService_ShouldRunStartupChecks_ShouldHaveNoParameters()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ShouldRunStartupChecks");

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().BeEmpty();
    }
} 