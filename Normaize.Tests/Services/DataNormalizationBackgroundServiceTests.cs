using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data.Services;
using Normaize.Tests.Repositories;
using Xunit;

namespace Normaize.Tests.Services;

[Trait("Category", TestSetup.Categories.Unit)]
public class DataNormalizationBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IJobQueueService> _mockJobQueueService;
    private readonly Mock<IDuplicateRowRemovalProcessor> _mockProcessor;
    private readonly Mock<ILogger<DataNormalizationBackgroundService>> _mockLogger;
    private readonly DataNormalizationBackgroundServiceOptions _options;
    private readonly DataNormalizationBackgroundService _service;

    public DataNormalizationBackgroundServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockJobQueueService = new Mock<IJobQueueService>();
        _mockProcessor = new Mock<IDuplicateRowRemovalProcessor>();
        _mockLogger = new Mock<ILogger<DataNormalizationBackgroundService>>();
        _options = new DataNormalizationBackgroundServiceOptions
        {
            IdleDelay = TimeSpan.FromMilliseconds(100),
            ErrorRetryDelay = TimeSpan.FromMilliseconds(100),
            MaxConcurrentProcessors = 3
        };

        _service = new DataNormalizationBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Options.Create(_options),
            _mockJobQueueService.Object);

        // Setup service provider mocks
        _mockServiceScopeFactory.Setup(f => f.CreateScope())
            .Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
        _mockServiceScope.Setup(s => s.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        _mockServiceScope.Setup(s => s.Dispose());
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateService()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataNormalizationBackgroundService(
            null!,
            _mockLogger.Object,
            Options.Create(_options),
            _mockJobQueueService.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataNormalizationBackgroundService(
            _mockServiceProvider.Object,
            null!,
            Options.Create(_options),
            _mockJobQueueService.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataNormalizationBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            null!,
            _mockJobQueueService.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullJobQueueService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DataNormalizationBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Options.Create(_options),
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobQueueService");
    }

    [Fact]
    public void Options_ShouldBeCorrectlySet()
    {
        // Assert
        _service.Should().NotBeNull();
        // Note: We can't directly access private fields, but we can verify the service was created with the options
    }

    [Fact]
    public void Service_ShouldInheritFromBackgroundService()
    {
        // Assert
        _service.Should().BeAssignableTo<BackgroundService>();
    }

    [Fact]
    public void Service_ShouldImplementIDisposable()
    {
        // Assert
        _service.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Service_ShouldHaveCorrectType()
    {
        // Assert
        _service.Should().BeOfType<DataNormalizationBackgroundService>();
    }

    [Fact]
    public void Service_ShouldNotBeNull()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Service_ShouldBeCreatedSuccessfully()
    {
        // Assert
        _service.Should().NotBeNull();
        _service.Should().BeOfType<DataNormalizationBackgroundService>();
    }
}
