using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using System;
using System.Linq;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries;

public class GetAllAnalysesQueryHandlerTests
{
    private readonly Mock<IAnalysisRepository> _analysisRepositoryMock;
    private readonly IAnalysisMapper _mapper;
    private readonly GetAllAnalysesQueryHandler _handler;

    public GetAllAnalysesQueryHandlerTests()
    {
        _analysisRepositoryMock = new Mock<IAnalysisRepository>();
        _mapper = new FakeAnalysisMapper();
        _handler = new GetAllAnalysesQueryHandler(_analysisRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnTotalItemsAndPagedItems()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        var allAnalyses = Enumerable.Range(1, 25)
            .Select(i => Analysis.Create(
                name: $"analysis-{i:00}",
                description: null,
                type: AnalysisType.Statistical,
                dataSetId: dataSetId))
            .ToList();

        _analysisRepositoryMock
            .Setup(r => r.GetByCriteriaAsync(dataSetId, null, null, false))
            .ReturnsAsync(allAnalyses);

        var query = new GetAllAnalysesQuery(PageNumber: 3, PageSize: 10, DataSetId: dataSetId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.TotalItems.Should().Be(25);
        result.Items.Should().HaveCount(5);

        _analysisRepositoryMock.Verify(r => r.GetByCriteriaAsync(dataSetId, null, null, false), Times.Once);
    }

    private sealed class FakeAnalysisMapper : IAnalysisMapper
    {
        public AnalysisDto ToDto(Analysis analysis)
        {
            return new AnalysisDto
            {
                Id = Guid.NewGuid(),
                Name = analysis.Name,
                Description = analysis.Description,
                Type = analysis.Type,
                Status = analysis.Status,
                CreatedAt = analysis.CreatedAt,
                DataSetId = analysis.DataSetId,
                ComparisonDataSetId = analysis.ComparisonDataSetId,
                IsDeleted = analysis.IsDeleted
            };
        }

        public AnalysisResultDto ToResultDto(Analysis analysis)
        {
            throw new NotSupportedException();
        }

        public Analysis FromCreateDto(CreateAnalysisDto dto)
        {
            throw new NotSupportedException();
        }
    }
}
