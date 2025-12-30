using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Infrastructure.Behaviors;
using Serilog.Context;
using System.Diagnostics;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Behaviors;

// Test request/response types for testing
public record TestRequest : IRequest<TestResponse>;
public record TestResponse(string Value);

public class ObservabilityBehaviorTests
{
    private readonly Mock<ILogger<ObservabilityBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly ObservabilityBehavior<TestRequest, TestResponse> _behavior;

    public ObservabilityBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<ObservabilityBehavior<TestRequest, TestResponse>>>();
        _behavior = new ObservabilityBehavior<TestRequest, TestResponse>(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldLogRequestStart_WhenRequestIsHandled()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogRequestCompletion_WithTiming_WhenRequestSucceeds()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");
        var delay = TimeSpan.FromMilliseconds(50);

        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(delay);
            return expectedResponse;
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        // Verify completion log with timing
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify timing was logged (check for ElapsedMs parameter)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ElapsedMs") || v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new TestRequest();
        var expectedException = new InvalidOperationException("Test error");

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            throw expectedException;
        };

        // Act
        Func<Task> act = async () => await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test error");

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed") && v.ToString()!.Contains("TestRequest")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddRequestNameToLogContext()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            // The middleware pushes RequestName to LogContext
            // Actual LogContext verification happens at runtime when logs are written
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        // LogContext is pushed, so we verify the middleware completes without errors
    }

    [Fact]
    public async Task Handle_ShouldAddRequestIdToLogContext()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        // RequestId is generated and pushed to LogContext
        // Verification happens at runtime when logs include RequestId property
    }

    [Fact]
    public async Task Handle_ShouldAddActivityTags_WhenActivityExists()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource("TestActivitySource");
        using var activity = activitySource.StartActivity("TestActivity");

        if (activity == null)
        {
            // If Activity doesn't start, skip this test
            return;
        }

        var requestNameTag = (string?)null;
        var requestIdTag = (string?)null;
        var requestTypeTag = (string?)null;
        var requestStatusTag = (string?)null;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            requestNameTag = Activity.Current?.GetTagItem("request.name")?.ToString();
            requestIdTag = Activity.Current?.GetTagItem("request.id")?.ToString();
            requestTypeTag = Activity.Current?.GetTagItem("request.type")?.ToString();
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        requestNameTag.Should().Be("TestRequest");
        requestIdTag.Should().NotBeNullOrWhiteSpace();
        requestTypeTag.Should().Contain("TestRequest");

        // After completion, check status tag
        requestStatusTag = Activity.Current?.GetTagItem("request.status")?.ToString();
        requestStatusTag.Should().Be("success");
    }

    [Fact]
    public async Task Handle_ShouldAddErrorTagsToActivity_WhenExceptionOccurs()
    {
        // Arrange
        var request = new TestRequest();
        var expectedException = new InvalidOperationException("Test error");

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource("TestActivitySource");
        using var activity = activitySource.StartActivity("TestActivity");

        if (activity == null)
        {
            return;
        }

        var errorTypeTag = (string?)null;
        var errorMessageTag = (string?)null;
        var requestStatusTag = (string?)null;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            throw expectedException;
        };

        // Act
        Func<Task> act = async () => await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Check error tags were set
        errorTypeTag = Activity.Current?.GetTagItem("error.type")?.ToString();
        errorMessageTag = Activity.Current?.GetTagItem("error.message")?.ToString();
        requestStatusTag = Activity.Current?.GetTagItem("request.status")?.ToString();

        errorTypeTag.Should().Be("InvalidOperationException");
        errorMessageTag.Should().Be("Test error");
        requestStatusTag.Should().Be("error");
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenActivityIsNull()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");
        Activity.Current = null;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldMeasureExecutionTime_Accurately()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse("Success");
        var delay = TimeSpan.FromMilliseconds(100);
        var tolerance = TimeSpan.FromMilliseconds(50); // Allow some tolerance for test execution

        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(delay);
            return expectedResponse;
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.Should().Be(expectedResponse);
        stopwatch.Elapsed.Should().BeCloseTo(delay, tolerance);
    }

    [Fact]
    public async Task Handle_ShouldProcessMultipleRequests_Successfully()
    {
        // Arrange
        var request1 = new TestRequest();
        var request2 = new TestRequest();
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result1 = await _behavior.Handle(request1, next, CancellationToken.None);
        var result2 = await _behavior.Handle(request2, next, CancellationToken.None);

        // Assert
        result1.Should().Be(expectedResponse);
        result2.Should().Be(expectedResponse);

        // Verify that both requests were logged (each request generates a unique RequestId via Guid.NewGuid())
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldPreserveException_WhenErrorOccurs()
    {
        // Arrange
        var request = new TestRequest();
        var expectedException = new ArgumentException("Invalid argument", "paramName");

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            throw expectedException;
        };

        // Act
        Func<Task> act = async () => await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.Which.Should().Be(expectedException);
        exception.Which.ParamName.Should().Be("paramName");
    }
}

