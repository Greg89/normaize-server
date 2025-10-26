using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.API.Tests.Controllers;

public class DataNormalizationControllerTests
{
    private readonly Mock<ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>> _submitJobHandlerMock;
    private readonly Mock<ICommandHandler<RetryJobCommand>> _retryJobHandlerMock;
    private readonly Mock<ICommandHandler<CancelJobCommand>> _cancelJobHandlerMock;
    private readonly Mock<IQueryHandler<GetJobStatusQuery, JobStatusDto?>> _getJobStatusHandlerMock;
    private readonly Mock<ILogger<DataNormalizationController>> _loggerMock;
    private readonly DataNormalizationController _controller;

    public DataNormalizationControllerTests()
    {
        _submitJobHandlerMock = new Mock<ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid>>();
        _retryJobHandlerMock = new Mock<ICommandHandler<RetryJobCommand>>();
        _cancelJobHandlerMock = new Mock<ICommandHandler<CancelJobCommand>>();
        _getJobStatusHandlerMock = new Mock<IQueryHandler<GetJobStatusQuery, JobStatusDto?>>();
        _loggerMock = new Mock<ILogger<DataNormalizationController>>();
        
        _controller = new DataNormalizationController(
            _submitJobHandlerMock.Object,
            _retryJobHandlerMock.Object,
            _cancelJobHandlerMock.Object,
            _getJobStatusHandlerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Controller_ShouldBeProperlyConfigured()
    {
        // Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<DataNormalizationController>();
    }
}