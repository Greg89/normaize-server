using FluentAssertions;
using Normaize.Core.Interfaces;
using Xunit;

namespace Normaize.Tests.Services;

public class IDataProcessingInfrastructureTests
{
    [Fact]
    public void IDataProcessingInfrastructure_ShouldDefineRequiredProperties()
    {
        // Arrange & Act
        var interfaceType = typeof(IDataProcessingInfrastructure);

        // Assert
        interfaceType.GetProperty("Logger")!.Should().NotBeNull();
        interfaceType.GetProperty("Cache")!.Should().NotBeNull();
        interfaceType.GetProperty("StructuredLogging")!.Should().NotBeNull();
        interfaceType.GetProperty("ChaosEngineering")!.Should().NotBeNull();
        interfaceType.GetProperty("CacheExpiration")!.Should().NotBeNull();
        interfaceType.GetProperty("DefaultTimeout")!.Should().NotBeNull();
        interfaceType.GetProperty("QuickTimeout")!.Should().NotBeNull();
    }

    [Fact]
    public void IDataProcessingInfrastructure_PropertiesShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);

        // Assert
        interfaceType.GetProperty("Logger")!.PropertyType.Should().Be(typeof(Microsoft.Extensions.Logging.ILogger));
        interfaceType.GetProperty("Cache")!.PropertyType.Should().Be(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache));
        interfaceType.GetProperty("StructuredLogging")!.PropertyType.Should().Be(typeof(IStructuredLoggingService));
        interfaceType.GetProperty("ChaosEngineering")!.PropertyType.Should().Be(typeof(IChaosEngineeringService));
        interfaceType.GetProperty("CacheExpiration")!.PropertyType.Should().Be(typeof(TimeSpan));
        interfaceType.GetProperty("DefaultTimeout")!.PropertyType.Should().Be(typeof(TimeSpan));
        interfaceType.GetProperty("QuickTimeout")!.PropertyType.Should().Be(typeof(TimeSpan));
    }

    [Fact]
    public void IDataProcessingInfrastructure_ShouldBePublic()
    {
        // Assert
        typeof(IDataProcessingInfrastructure).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IDataProcessingInfrastructure_ShouldBeInterface()
    {
        // Assert
        typeof(IDataProcessingInfrastructure).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IDataProcessingInfrastructure_PropertiesShouldBeReadOnly()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);

        // Assert
        interfaceType.GetProperty("Logger")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("Cache")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("StructuredLogging")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("ChaosEngineering")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("CacheExpiration")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("DefaultTimeout")!.CanWrite.Should().BeFalse();
        interfaceType.GetProperty("QuickTimeout")!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void IDataProcessingInfrastructure_PropertiesShouldBeReadable()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);

        // Assert
        interfaceType.GetProperty("Logger")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("Cache")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("StructuredLogging")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("ChaosEngineering")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("CacheExpiration")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("DefaultTimeout")!.CanRead.Should().BeTrue();
        interfaceType.GetProperty("QuickTimeout")!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IDataProcessingInfrastructure_Logger_ShouldReturnILogger()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("Logger");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(Microsoft.Extensions.Logging.ILogger));
    }

    [Fact]
    public void IDataProcessingInfrastructure_Cache_ShouldReturnIMemoryCache()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("Cache");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache));
    }

    [Fact]
    public void IDataProcessingInfrastructure_StructuredLogging_ShouldReturnIStructuredLoggingService()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("StructuredLogging");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(IStructuredLoggingService));
    }

    [Fact]
    public void IDataProcessingInfrastructure_ChaosEngineering_ShouldReturnIChaosEngineeringService()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("ChaosEngineering");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(IChaosEngineeringService));
    }

    [Fact]
    public void IDataProcessingInfrastructure_CacheExpiration_ShouldReturnTimeSpan()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("CacheExpiration");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(TimeSpan));
    }

    [Fact]
    public void IDataProcessingInfrastructure_DefaultTimeout_ShouldReturnTimeSpan()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("DefaultTimeout");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(TimeSpan));
    }

    [Fact]
    public void IDataProcessingInfrastructure_QuickTimeout_ShouldReturnTimeSpan()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);
        var property = interfaceType.GetProperty("QuickTimeout");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(TimeSpan));
    }

    [Fact]
    public void IDataProcessingInfrastructure_ShouldHaveSevenProperties()
    {
        // Arrange
        var interfaceType = typeof(IDataProcessingInfrastructure);

        // Act
        var properties = interfaceType.GetProperties();

        // Assert
        properties.Should().HaveCount(7);
    }
} 