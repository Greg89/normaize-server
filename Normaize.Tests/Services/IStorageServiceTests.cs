using FluentAssertions;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Xunit;

namespace Normaize.Tests.Services;

public class IStorageServiceTests
{
    [Fact]
    public void IStorageService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IStorageService);

        // Assert
        interfaceType.GetMethod("SaveFileAsync", new[] { typeof(FileUploadRequest) })!.Should().NotBeNull();
        interfaceType.GetMethod("GetFileAsync", new[] { typeof(string) })!.Should().NotBeNull();
        interfaceType.GetMethod("DeleteFileAsync", new[] { typeof(string) })!.Should().NotBeNull();
        interfaceType.GetMethod("FileExistsAsync", new[] { typeof(string) })!.Should().NotBeNull();
    }

    [Fact]
    public void IStorageService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);

        // Assert
        interfaceType.GetMethod("SaveFileAsync", new[] { typeof(FileUploadRequest) })!.ReturnType.Should().Be(typeof(Task<string>));
        interfaceType.GetMethod("GetFileAsync", new[] { typeof(string) })!.ReturnType.Should().Be(typeof(Task<Stream>));
        interfaceType.GetMethod("DeleteFileAsync", new[] { typeof(string) })!.ReturnType.Should().Be(typeof(Task));
        interfaceType.GetMethod("FileExistsAsync", new[] { typeof(string) })!.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void IStorageService_ShouldBePublic()
    {
        // Assert
        typeof(IStorageService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IStorageService_ShouldBeInterface()
    {
        // Assert
        typeof(IStorageService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IStorageService_MethodsShouldBeAsync()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);

        // Assert
        interfaceType.GetMethod("SaveFileAsync", new[] { typeof(FileUploadRequest) })!.ReturnType.Name.Should().Contain("Task");
        interfaceType.GetMethod("GetFileAsync", new[] { typeof(string) })!.ReturnType.Name.Should().Contain("Task");
        interfaceType.GetMethod("DeleteFileAsync", new[] { typeof(string) })!.ReturnType.Name.Should().Contain("Task");
        interfaceType.GetMethod("FileExistsAsync", new[] { typeof(string) })!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void IStorageService_SaveFileAsync_ShouldAcceptFileUploadRequest()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("SaveFileAsync", new[] { typeof(FileUploadRequest) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(FileUploadRequest));
    }

    [Fact]
    public void IStorageService_GetFileAsync_ShouldAcceptString()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("GetFileAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void IStorageService_DeleteFileAsync_ShouldAcceptString()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("DeleteFileAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void IStorageService_FileExistsAsync_ShouldAcceptString()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("FileExistsAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void IStorageService_SaveFileAsync_ShouldReturnString()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("SaveFileAsync", new[] { typeof(FileUploadRequest) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<string>));
    }

    [Fact]
    public void IStorageService_GetFileAsync_ShouldReturnStream()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("GetFileAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Stream>));
    }

    [Fact]
    public void IStorageService_DeleteFileAsync_ShouldReturnTask()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("DeleteFileAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void IStorageService_FileExistsAsync_ShouldReturnTaskOfBool()
    {
        // Arrange
        var interfaceType = typeof(IStorageService);
        var method = interfaceType.GetMethod("FileExistsAsync", new[] { typeof(string) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
    }
} 