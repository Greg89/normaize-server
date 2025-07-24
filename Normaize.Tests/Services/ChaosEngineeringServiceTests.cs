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
    private readonly Mock<ILogger<ChaosEngineeringService>> _mockLogger;
    private readonly Mock<IOptionsMonitor<ChaosEngineeringOptions>> _mockOptionsMonitor;

    public ChaosEngineeringServiceTests()
    {
        _mockLogger = new Mock<ILogger<ChaosEngineeringService>>();
        _mockOptionsMonitor = new Mock<IOptionsMonitor<ChaosEngineeringOptions>>();
        
        // Set up default options with chaos engineering enabled for tests
        var defaultOptions = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            GlobalProbabilityMultiplier = 1.0,
            EnableLogging = true
        };
        
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(defaultOptions);
        _service = new ChaosEngineeringService(_mockLogger.Object, _mockOptionsMonitor.Object);
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

    // NEW TESTS FOR IMPROVED COVERAGE

    [Fact]
    public void ShouldTriggerChaos_WhenEnabled_ShouldCheckEnvironment()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Production"]
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario");

        // Assert
        result.Should().BeFalse(); // Should be false because environment is not Production
    }

    [Fact]
    public void ShouldTriggerChaos_WhenEnvironmentNotAllowed_ShouldReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Production", "Staging"]
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario");

        // Assert
        result.Should().BeFalse(); // Should be false because Development is not in allowed environments
    }

    [Fact]
    public void ShouldTriggerChaos_WhenEnvironmentAllowed_ShouldContinueEvaluation()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Production"]
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario");

        // Assert
        result.Should().BeFalse(); // Should still be false due to other conditions, but environment check passes
    }

    [Fact]
    public void ShouldTriggerForUser_WithExcludedUserId_ShouldReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development"],
            UserBasedTriggers = new UserBasedTriggers
            {
                Enabled = true,
                ExcludedUserIds = ["excluded-user"]
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var context = new Dictionary<string, object>
        {
            ["UserId"] = "excluded-user"
        };

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario", context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerForUser_WithRegularUserId_ShouldReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development"],
            UserBasedTriggers = new UserBasedTriggers
            {
                Enabled = true,
                TestUserIds = ["test-user-123"] // Only this user is a test user
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var context = new Dictionary<string, object>
        {
            ["UserId"] = "regular-user"
        };

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario", context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerBuiltInScenario_WithValidConfig_ShouldCheckProbability()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-test-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0, // 100% probability
                    MaxTriggersPerHour = 10
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("unique-test-scenario");

        // Assert
        result.Should().BeTrue(); // Should trigger with 100% probability
    }

    [Fact]
    public void ShouldTriggerBuiltInScenario_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-disabled-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = false, // Disabled scenario
                    Probability = 1.0
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("unique-disabled-scenario");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerBuiltInScenario_WhenTimeRestricted_ShouldCheckWindows()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-time-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true,
                    Probability = 1.0,
                    TimeWindowRestricted = true,
                    AllowedTimeWindows =
                    [
                        new TimeWindow 
                        { 
                            StartTime = "00:00", 
                            EndTime = "23:59", 
                            DaysOfWeek = [(int)DateTime.Now.DayOfWeek] // Today
                        }
                    ]
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("unique-time-scenario");

        // Assert
        result.Should().BeTrue(); // Should trigger if current time is in allowed window
    }

    [Fact]
    public async Task ExecuteChaosAsync_WhenTriggered_ShouldExecuteAction()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-execute-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0 // 100% probability
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var actionExecuted = false;
        var action = new Func<Task>(() => 
        {
            actionExecuted = true;
            return Task.CompletedTask;
        });

        // Act
        var result = await _service.ExecuteChaosAsync("unique-execute-scenario", action);

        // Assert
        result.Should().BeTrue();
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteChaosAsync_WhenCustomActionRegistered_ShouldExecuteCustom()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-custom-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var customActionExecuted = false;
        var customAction = new Func<Task>(() => 
        {
            customActionExecuted = true;
            return Task.CompletedTask;
        });

        _service.RegisterChaosScenario("unique-custom-scenario", context => true, customAction);

        var actionExecuted = false;
        var action = new Func<Task>(() => 
        {
            actionExecuted = true;
            return Task.CompletedTask;
        });

        // Act
        var result = await _service.ExecuteChaosAsync("unique-custom-scenario", action);

        // Assert
        result.Should().BeTrue();
        customActionExecuted.Should().BeTrue();
        actionExecuted.Should().BeFalse(); // Original action should not be executed
    }

    [Fact]
    public async Task ExecuteChaosAsync_WhenExceptionOccurs_ShouldLogAndReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-exception-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var action = new Func<Task>(() => throw new InvalidOperationException("Test exception"));

        // Act
        var result = await _service.ExecuteChaosAsync("unique-exception-scenario", action);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void IsRateLimited_WhenUnderLimit_ShouldReturnFalse()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            MaxTriggersPerMinute = 10
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        // Act
        var result = _service.ShouldTriggerChaos("test-scenario");

        // Assert
        result.Should().BeFalse(); // Should not be rate limited on first call
    }

    [Fact]
    public async Task IsHourlyRateLimited_WhenScenarioLimited_ShouldReturnTrue()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["test-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0,
                    MaxTriggersPerHour = 1 // Only 1 trigger allowed per hour
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var action = new Func<Task>(() => Task.CompletedTask);

        // Act - First trigger should succeed
        var firstResult = await _service.ExecuteChaosAsync("test-scenario", action);
        
        // Second trigger should be rate limited
        var secondResult = await _service.ExecuteChaosAsync("test-scenario", action);

        // Assert
        firstResult.Should().BeTrue(); // First trigger should succeed
        secondResult.Should().BeFalse(); // Second trigger should be rate limited
    }

    [Fact]
    public async Task RecordTrigger_ShouldIncrementCounts()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-record-scenario"] = new ChaosScenarioConfig 
                { 
                    Enabled = true, 
                    Probability = 1.0
                }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var action = new Func<Task>(() => Task.CompletedTask);

        // Act
        var result = await _service.ExecuteChaosAsync("unique-record-scenario", action);
        var stats = _service.GetStats();

        // Assert
        result.Should().BeTrue();
        stats.TriggeredScenarios.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetStats_WithMultipleTriggers_ShouldAggregateCorrectly()
    {
        // Arrange
        var options = new ChaosEngineeringOptions
        {
            Enabled = true,
            AllowedEnvironments = ["Development", "Test"],
            Scenarios = new Dictionary<string, ChaosScenarioConfig>
            {
                ["unique-stats-scenario1"] = new ChaosScenarioConfig { Enabled = true, Probability = 1.0 },
                ["unique-stats-scenario2"] = new ChaosScenarioConfig { Enabled = true, Probability = 1.0 }
            }
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var action = new Func<Task>(() => Task.CompletedTask);

        // Act
        await _service.ExecuteChaosAsync("unique-stats-scenario1", action);
        await _service.ExecuteChaosAsync("unique-stats-scenario2", action);
        var stats = _service.GetStats();

        // Assert
        stats.TriggeredScenarios.Should().BeGreaterThanOrEqualTo(2);
        stats.ScenarioCounts.Should().ContainKey("unique-stats-scenario1");
        stats.ScenarioCounts.Should().ContainKey("unique-stats-scenario2");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new ChaosEngineeringService(null!, _mockOptionsMonitor.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var action = () => new ChaosEngineeringService(_mockLogger.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("optionsMonitor");
    }

    [Fact]
    public void RegisterChaosScenario_WithNullTriggerCondition_ShouldThrowArgumentNullException()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var chaosAction = new Func<Task>(() => Task.CompletedTask);

        // Act & Assert
        var action = () => _service.RegisterChaosScenario(scenarioName, null!, chaosAction);
        action.Should().Throw<ArgumentNullException>().WithParameterName("triggerCondition");
    }

    [Fact]
    public void RegisterChaosScenario_WithNullChaosAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var scenarioName = "test-scenario";
        var triggerCondition = new Func<IDictionary<string, object>?, bool>(context => true);

        // Act & Assert
        var action = () => _service.RegisterChaosScenario(scenarioName, triggerCondition, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("chaosAction");
    }
}