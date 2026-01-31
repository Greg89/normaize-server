using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries;

public class GetUserJobsQueryHandlerTests
{
    private readonly Mock<INormalizationJobRepository> _jobRepositoryMock;
    private readonly GetUserJobsQueryHandler _handler;

    public GetUserJobsQueryHandlerTests()
    {
        _jobRepositoryMock = new Mock<INormalizationJobRepository>();
        _handler = new GetUserJobsQueryHandler(_jobRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTotalItemsAndPagedItems_AndMapCompletedToSucceeded()
    {
        // Arrange
        var userId = "user-123";

        var jobs = new List<NormalizationJob>
        {
            NormalizationJob.Create(Guid.NewGuid(), "RemoveDuplicates", "{}"),
            NormalizationJob.Create(Guid.NewGuid(), "RemoveDuplicates", "{}"),
        };

        JobStatus? capturedStatus = null;

        _jobRepositoryMock
            .Setup(r => r.GetJobsForUserAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<JobStatus?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Callback<string, Guid?, JobStatus?, string?, DateTime?, DateTime?, int, int>((_, _, status, _, _, _, _, _) =>
            {
                capturedStatus = status;
            })
            .ReturnsAsync((jobs, 25));

        var query = new GetUserJobsQuery(
            UserId: userId,
            PageNumber: 2,
            PageSize: 10,
            DataSetId: null,
            Status: "Completed",
            JobType: null,
            StartDate: null,
            EndDate: null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        capturedStatus.Should().Be(JobStatus.Succeeded);
        result.TotalItems.Should().Be(25);
        result.Items.Should().HaveCount(2);
        result.Items[0].OperationType.Should().Be("RemoveDuplicates");

        _jobRepositoryMock.Verify(r => r.GetJobsForUserAsync(
            userId,
            null,
            It.IsAny<JobStatus?>(),
            null,
            null,
            null,
            2,
            10), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var query = new GetUserJobsQuery(
            UserId: "user-123",
            PageNumber: 1,
            PageSize: 20,
            Status: "NotARealStatus");

        // Act
        Func<Task> act = () => _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
