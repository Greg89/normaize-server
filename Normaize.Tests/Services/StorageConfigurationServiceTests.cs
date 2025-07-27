using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Data.Services;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class StorageConfigurationServiceTests
{
    private readonly Mock<IAppConfigurationService> _mockAppConfigService;
    private readonly StorageConfiguration _configuration;
    private readonly StorageConfigurationService _service;

    public StorageConfigurationServiceTests()
    {
        _mockAppConfigService = new Mock<IAppConfigurationService>();
        _configuration = new StorageConfiguration();
        var options = Options.Create(_configuration);
        _service = new StorageConfigurationService(_mockAppConfigService.Object, options);
    }

    [Fact]
    public void GetConfiguration_ReturnsConfiguration()
    {
        // Act
        var result = _service.GetConfiguration();

        // Assert
        result.Should().Be(_configuration);
    }

    [Fact]
    public void GetDiagnostics_WithFullS3Config_ReturnsCorrectDiagnostics()
    {
        // Arrange
        _configuration.Provider = "S3";
        _configuration.S3Bucket = "test-bucket";
        _configuration.S3AccessKey = "test-key";
        _configuration.S3SecretKey = "test-secret";
        _configuration.S3ServiceUrl = "https://s3.test.com";
        _mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Production");

        // Act
        var result = _service.GetDiagnostics();

        // Assert
        result.Should().NotBeNull();
        result.StorageProvider.Should().Be(StorageProvider.S3);
        result.S3Configured.Should().BeTrue();
        result.S3Bucket.Should().Be(AppConstants.ConfigStatus.SET);
        result.S3AccessKey.Should().Be(AppConstants.ConfigStatus.SET);
        result.S3SecretKey.Should().Be(AppConstants.ConfigStatus.SET);
        result.S3ServiceUrl.Should().Be(AppConstants.ConfigStatus.SET);
        result.Environment.Should().Be("Production");
    }

    [Fact]
    public void GetDiagnostics_WithMissingS3Config_ReturnsNotSetStatus()
    {
        // Arrange
        _configuration.Provider = "S3";
        _configuration.S3Bucket = null;
        _configuration.S3AccessKey = null;
        _configuration.S3SecretKey = null;
        _configuration.S3ServiceUrl = null;
        _mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");

        // Act
        var result = _service.GetDiagnostics();

        // Assert
        result.Should().NotBeNull();
        result.StorageProvider.Should().Be(StorageProvider.S3);
        result.S3Configured.Should().BeFalse();
        result.S3Bucket.Should().Be(AppConstants.ConfigStatus.NOT_SET);
        result.S3AccessKey.Should().Be(AppConstants.ConfigStatus.NOT_SET);
        result.S3SecretKey.Should().Be(AppConstants.ConfigStatus.NOT_SET);
        result.S3ServiceUrl.Should().Be(AppConstants.ConfigStatus.NOT_SET);
        result.Environment.Should().Be("Development");
    }

    [Fact]
    public void GetDiagnostics_WithEmptyEnvironment_ReturnsNotSetStatus()
    {
        // Arrange
        _configuration.Provider = "Local";
        _mockAppConfigService.Setup(x => x.GetEnvironment()).Returns(string.Empty);

        // Act
        var result = _service.GetDiagnostics();

        // Assert
        result.Should().NotBeNull();
        result.StorageProvider.Should().Be(StorageProvider.Local);
        result.Environment.Should().Be(AppConstants.ConfigStatus.NOT_SET);
    }

    [Theory]
    [InlineData("s3", StorageProvider.S3)]
    [InlineData("S3", StorageProvider.S3)]
    [InlineData("azure", StorageProvider.Azure)]
    [InlineData("Azure", StorageProvider.Azure)]
    [InlineData("memory", StorageProvider.Memory)]
    [InlineData("Memory", StorageProvider.Memory)]
    [InlineData("local", StorageProvider.Local)]
    [InlineData("Local", StorageProvider.Local)]
    [InlineData("unknown", StorageProvider.Local)]
    [InlineData("", StorageProvider.Local)]
    [InlineData(null, StorageProvider.Local)]
    public void GetStorageProvider_WithDifferentProviders_ReturnsCorrectProvider(string? provider, StorageProvider expected)
    {
        // Arrange
        _configuration.Provider = provider ?? string.Empty;

        // Act
        var result = _service.GetStorageProvider();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsS3Configured_WithFullConfig_ReturnsTrue()
    {
        // Arrange
        _configuration.S3Bucket = "test-bucket";
        _configuration.S3AccessKey = "test-key";
        _configuration.S3SecretKey = "test-secret";

        // Act
        var result = _service.IsS3Configured();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "key", "secret")]
    [InlineData("bucket", null, "secret")]
    [InlineData("bucket", "key", null)]
    [InlineData("", "key", "secret")]
    [InlineData("bucket", "", "secret")]
    [InlineData("bucket", "key", "")]
    public void IsS3Configured_WithMissingConfig_ReturnsFalse(string? bucket, string? key, string? secret)
    {
        // Arrange
        _configuration.S3Bucket = bucket;
        _configuration.S3AccessKey = key;
        _configuration.S3SecretKey = secret;

        // Act
        var result = _service.IsS3Configured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAzureConfigured_WithFullConfig_ReturnsTrue()
    {
        // Arrange
        _configuration.AzureConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";
        _configuration.AzureContainer = "test-container";

        // Act
        var result = _service.IsAzureConfigured();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "container")]
    [InlineData("connection", null)]
    [InlineData("", "container")]
    [InlineData("connection", "")]
    public void IsAzureConfigured_WithMissingConfig_ReturnsFalse(string? connectionString, string? container)
    {
        // Arrange
        _configuration.AzureConnectionString = connectionString;
        _configuration.AzureContainer = container;

        // Act
        var result = _service.IsAzureConfigured();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetDiagnostics_WithAzureConfig_ReturnsCorrectDiagnostics()
    {
        // Arrange
        _configuration.Provider = "Azure";
        _configuration.AzureConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";
        _configuration.AzureContainer = "test-container";
        _mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Staging");

        // Act
        var result = _service.GetDiagnostics();

        // Assert
        result.Should().NotBeNull();
        result.StorageProvider.Should().Be(StorageProvider.Azure);
        result.Environment.Should().Be("Staging");
    }

    [Fact]
    public void GetDiagnostics_WithMemoryConfig_ReturnsCorrectDiagnostics()
    {
        // Arrange
        _configuration.Provider = "Memory";
        _mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Test");

        // Act
        var result = _service.GetDiagnostics();

        // Assert
        result.Should().NotBeNull();
        result.StorageProvider.Should().Be(StorageProvider.Memory);
        result.Environment.Should().Be("Test");
    }
}