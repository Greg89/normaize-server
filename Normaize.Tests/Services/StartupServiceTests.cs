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
        interfaceType.GetMethod("ConfigureStartupAsync", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("ShouldRunStartupChecks")!.Should().NotBeNull();
        interfaceType.GetMethod("ApplyMigrationsAsync", [typeof(CancellationToken)])!.Should().NotBeNull();
        interfaceType.GetMethod("PerformHealthChecksAsync", [typeof(CancellationToken)])!.Should().NotBeNull();
    }

    [Fact]
    public void IStartupService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);

        // Assert
        interfaceType.GetMethod("ConfigureStartupAsync", [typeof(CancellationToken)])!.ReturnType.Should().Be<Task>();
        interfaceType.GetMethod("ShouldRunStartupChecks")!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("ApplyMigrationsAsync", [typeof(CancellationToken)])!.ReturnType.Should().Be<Task>();
        interfaceType.GetMethod("PerformHealthChecksAsync", [typeof(CancellationToken)])!.ReturnType.Should().Be<Task>();
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
        var method = interfaceType.GetMethod("ConfigureStartupAsync", [typeof(CancellationToken)]);

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
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IStartupService_ApplyMigrationsAsync_ShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ApplyMigrationsAsync", [typeof(CancellationToken)]);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStartupService_PerformHealthChecksAsync_ShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("PerformHealthChecksAsync", [typeof(CancellationToken)]);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStartupService_ConfigureStartupAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ConfigureStartupAsync", [typeof(CancellationToken)]);

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be<CancellationToken>();
    }

    [Fact]
    public void IStartupService_ApplyMigrationsAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("ApplyMigrationsAsync", [typeof(CancellationToken)]);

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be<CancellationToken>();
    }

    [Fact]
    public void IStartupService_PerformHealthChecksAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IStartupService);
        var method = interfaceType.GetMethod("PerformHealthChecksAsync", [typeof(CancellationToken)]);

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be<CancellationToken>();
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