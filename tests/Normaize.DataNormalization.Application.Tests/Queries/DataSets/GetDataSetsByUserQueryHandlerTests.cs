using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Queries.DataSets;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using System.Linq;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries.DataSets;

public class GetDataSetsByUserQueryHandlerTests
{
    private readonly Mock<IDataSetRepository> _dataSetRepositoryMock;
    private readonly GetDataSetsByUserQueryHandler _handler;

    public GetDataSetsByUserQueryHandlerTests()
    {
        _dataSetRepositoryMock = new Mock<IDataSetRepository>();
        _handler = new GetDataSetsByUserQueryHandler(_dataSetRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTotalItemsAndPagedItems()
    {
        // Arrange
        const string userId = "auth0|user";

        var allDataSets = Enumerable.Range(1, 25)
            .Select(i => Domain.Entities.DataSet.Create(
                name: $"ds-{i:00}",
                description: null,
                userId: userId,
                fileInfo: FileMetadata.CreateFromFileName($"file-{i:00}.csv", $"s3://bucket/file-{i:00}.csv", 123)))
            .ToList();

        _dataSetRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allDataSets);

        var request = new GetDataSetsByUserQuery(UserId: userId, Page: 2, PageSize: 10, IncludeDeleted: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.TotalItems.Should().Be(25);
        result.Items.Should().HaveCount(10);

        _dataSetRepositoryMock.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _dataSetRepositoryMock.Verify(r => r.GetAllByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
