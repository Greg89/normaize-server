using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Queries.DataSets;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using System.Linq;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries.DataSets;

public class SearchDataSetsQueryHandlerTests
{
    private readonly Mock<IDataSetRepository> _dataSetRepositoryMock;
    private readonly SearchDataSetsQueryHandler _handler;

    public SearchDataSetsQueryHandlerTests()
    {
        _dataSetRepositoryMock = new Mock<IDataSetRepository>();
        _handler = new SearchDataSetsQueryHandler(_dataSetRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTotalItemsFromFullFilteredSet_NotJustCurrentPage()
    {
        // Arrange
        const string userId = "auth0|user";

        var matching = Enumerable.Range(1, 15)
            .Select(i => Domain.Entities.DataSet.Create(
                name: $"alpha-{i:00}",
                description: "contains alpha",
                userId: userId,
                fileInfo: FileMetadata.CreateFromFileName($"a-{i:00}.csv", $"s3://bucket/a-{i:00}.csv", 123)))
            .ToList();

        var nonMatching = Enumerable.Range(1, 5)
            .Select(i => Domain.Entities.DataSet.Create(
                name: $"beta-{i:00}",
                description: "does not match",
                userId: userId,
                fileInfo: FileMetadata.CreateFromFileName($"b-{i:00}.csv", $"s3://bucket/b-{i:00}.csv", 123)))
            .ToList();

        // Soft-delete one matching dataset; it should not count or return.
        matching[0].Delete("tester");

        var allDataSets = matching.Concat(nonMatching).ToList();

        _dataSetRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allDataSets);

        var request = new SearchDataSetsQuery(SearchTerm: "alpha", UserId: userId, Page: 2, PageSize: 5);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.TotalItems.Should().Be(14);
        result.Items.Should().HaveCount(5);

        _dataSetRepositoryMock.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
