using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.Configuration;
using Normaize.Core.Interfaces;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class ChaosEngineeringServiceTests
{
    private readonly ChaosEngineeringService _service;

    public ChaosEngineeringServiceTests()
    {
        var mockLogger = new Mock<ILogger<ChaosEngineeringService>>();
        var mockOptions = new Mock<IOptionsMonitor<ChaosEngineeringOptions>>();
        mockOptions.Setup(x => x.CurrentValue).Returns(new ChaosEngineeringOptions());
        _service = new ChaosEngineeringService(mockLogger.Object, mockOptions.Object);
    }

    [Fact]
    public void IChaosEngineeringService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IChaosEngineeringService);

        // Assert
        interfaceType.GetMethod("ShouldTriggerChaos", [typeof(string), typeof(IDictionary<string, object>)])!.Should().NotBeNull();
        interfaceType.GetMethod("ShouldTriggerChaos", [typeof(string), typeof(string), typeof(string), typeof(IDictionary<string, object>)])!.Should().NotBeNull();
        interfaceType.GetMethod("ExecuteChaosAsync", [typeof(string), typeof(Func<Task>), typeof(IDictionary<string, object>)])!.Should().NotBeNull();
        interfaceType.GetMethod("ExecuteChaosAsync", [typeof(string), typeof(string), typeof(string), typeof(Func<Task>), typeof(IDictionary<string, object>)])!.Should().NotBeNull();
        // Remove Task<object> checks, as the interface uses generics for return value
        interfaceType.GetMethod("RegisterChaosScenario", [typeof(string), typeof(Func<IDictionary<string, object>?, bool>), typeof(Func<Task>)])!.Should().NotBeNull();
        interfaceType.GetMethod("GetStats")!.Should().NotBeNull();
    }

    [Fact]
    public void IChaosEngineeringService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IChaosEngineeringService);

        // Assert
        interfaceType.GetMethod("ShouldTriggerChaos", [typeof(string), typeof(IDictionary<string, object>)])!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("ShouldTriggerChaos", [typeof(string), typeof(string), typeof(string), typeof(IDictionary<string, object>)])!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("ExecuteChaosAsync", [typeof(string), typeof(Func<Task>), typeof(IDictionary<string, object>)])!.ReturnType.Should().Be<Task<bool>>();
        interfaceType.GetMethod("ExecuteChaosAsync", [typeof(string), typeof(string), typeof(string), typeof(Func<Task>), typeof(IDictionary<string, object>)])!.ReturnType.Should().Be<Task<bool>>();
        // Remove Task<object> checks, as the interface uses generics for return value
        interfaceType.GetMethod("RegisterChaosScenario", [typeof(string), typeof(Func<IDictionary<string, object>?, bool>), typeof(Func<Task>)])!.ReturnType.Should().Be(typeof(void));
        interfaceType.GetMethod("GetStats")!.ReturnType.Should().Be<ChaosEngineeringStats>();
    }

    [Fact]
    public void IChaosEngineeringService_ShouldBePublic()
    {
        // Assert
        typeof(IChaosEngineeringService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IChaosEngineeringService_ShouldBeInterface()
    {
        // Assert
        typeof(IChaosEngineeringService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerChaos_WithValidScenario_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";

        // Act
        var result = _service.ShouldTriggerChaos(scenarioName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerChaos_WithContext_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = _service.ShouldTriggerChaos(scenarioName, context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerChaos_WithCorrelationInfo_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var correlationId = "test-correlation";
        var operationName = "test-operation";
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = _service.ShouldTriggerChaos(scenarioName, correlationId, operationName, context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithValidScenario_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var action = new Func<Task>(() => Task.CompletedTask);

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, action);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithContext_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var action = new Func<Task>(() => Task.CompletedTask);
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, action, context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithCorrelationInfo_ShouldReturnFalseByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var correlationId = "test-correlation";
        var operationName = "test-operation";
        var action = new Func<Task>(() => Task.CompletedTask);
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, correlationId, operationName, action, context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithReturnValue_ShouldReturnDefaultByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var action = new Func<Task<string>>(() => Task.FromResult("test-result"));

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, action);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithReturnValueAndContext_ShouldReturnDefaultByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var action = new Func<Task<string>>(() => Task.FromResult("test-result"));
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, action, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WithReturnValueAndCorrelationInfo_ShouldReturnDefaultByDefault()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var correlationId = "test-correlation";
        var operationName = "test-operation";
        var action = new Func<Task<string>>(() => Task.FromResult("test-result"));
        var context = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = await _service.ExecuteChaosAsync(scenarioName, correlationId, operationName, action, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RegisterChaosScenario_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var triggerCondition = new Func<IDictionary<string, object>?, bool>(context => true);
        var chaosAction = new Func<Task>(() => Task.CompletedTask);

        // Act & Assert
        var action = () => _service.RegisterChaosScenario(scenarioName, triggerCondition, chaosAction);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetStats_ShouldReturnValidStats()
    {
        // Act
        var stats = _service.GetStats();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalScenarios.Should().BeGreaterThanOrEqualTo(0);
        stats.TriggeredScenarios.Should().BeGreaterThanOrEqualTo(0);
        stats.ScenarioCounts.Should().NotBeNull();
    }

    [Fact]
    public void ChaosEngineeringStats_ShouldHaveRequiredProperties()
    {
        // Arrange
        var stats = new ChaosEngineeringStats();

        // Assert
        stats.TotalScenarios.Should().Be(0);
        stats.TriggeredScenarios.Should().Be(0);
        stats.ScenarioCounts.Should().NotBeNull();
        stats.LastTriggered.Should().Be(default);
    }
} 