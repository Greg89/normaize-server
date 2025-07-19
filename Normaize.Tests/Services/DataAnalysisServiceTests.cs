using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace Normaize.Tests.Services;

public class DataAnalysisServiceTests
{
    private readonly Mock<IAnalysisRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<DataAnalysisService>> _mockLogger;
    private readonly DataAnalysisService _service;

    public DataAnalysisServiceTests()
    {
        _mockRepository = new Mock<IAnalysisRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<DataAnalysisService>>();
        
        _service = new DataAnalysisService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataAnalysisService(null!, _mockMapper.Object, _mockLogger.Object));
        
        exception.ParamName.Should().Be("analysisRepository");
    }

    [Fact]
    public void Constructor_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataAnalysisService(_mockRepository.Object, null!, _mockLogger.Object));
        
        exception.ParamName.Should().Be("mapper");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataAnalysisService(_mockRepository.Object, _mockMapper.Object, null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task CreateAnalysisAsync_WithValidDto_ShouldCreateAnalysis()
    {
        // Arrange
        var createDto = new CreateAnalysisDto
        {
            Name = "Test Analysis",
            Type = AnalysisType.Statistical,
            DataSetId = 1
        };

        var analysis = new Analysis { Id = 1, Name = "Test Analysis" };
        var analysisDto = new AnalysisDto { Id = 1, Name = "Test Analysis" };

        _mockMapper.Setup(m => m.Map<Analysis>(createDto)).Returns(analysis);
        _mockRepository.Setup(r => r.AddAsync(analysis)).ReturnsAsync(analysis);
        _mockMapper.Setup(m => m.Map<AnalysisDto>(analysis)).Returns(analysisDto);

        // Act
        var result = await _service.CreateAnalysisAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Analysis");
        
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Analysis>()), Times.Once);
        _mockMapper.Verify(m => m.Map<Analysis>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<AnalysisDto>(analysis), Times.Once);
    }

    [Fact]
    public async Task CreateAnalysisAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.CreateAnalysisAsync(null!));
        
        exception.ParamName.Should().Be("createDto");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAnalysisAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        // Arrange
        var createDto = new CreateAnalysisDto
        {
            Name = name!,
            Type = AnalysisType.Statistical,
            DataSetId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateAnalysisAsync(createDto));
        
        exception.Message.Should().Contain("Analysis name is required");
    }

    [Fact]
    public async Task CreateAnalysisAsync_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var createDto = new CreateAnalysisDto
        {
            Name = new string('a', 256), // 256 characters
            Type = AnalysisType.Statistical,
            DataSetId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateAnalysisAsync(createDto));
        
        exception.Message.Should().Contain("Analysis name cannot exceed 255 characters");
    }

    [Fact]
    public async Task CreateAnalysisAsync_WithInvalidDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var createDto = new CreateAnalysisDto
        {
            Name = "Test Analysis",
            Type = AnalysisType.Statistical,
            DataSetId = 0
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateAnalysisAsync(createDto));
        
        exception.Message.Should().Contain("Valid dataset ID is required");
    }

    [Fact]
    public async Task GetAnalysisAsync_WithExistingId_ShouldReturnAnalysis()
    {
        // Arrange
        var analysis = new Analysis { Id = 1, Name = "Test Analysis" };
        var analysisDto = new AnalysisDto { Id = 1, Name = "Test Analysis" };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);
        _mockMapper.Setup(m => m.Map<AnalysisDto>(analysis)).Returns(analysisDto);

        // Act
        var result = await _service.GetAnalysisAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Analysis");
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockMapper.Verify(m => m.Map<AnalysisDto>(analysis), Times.Once);
    }

    [Fact]
    public async Task GetAnalysisAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Analysis?)null);

        // Act
        var result = await _service.GetAnalysisAsync(999);

        // Assert
        result.Should().BeNull();
        
        _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        _mockMapper.Verify(m => m.Map<AnalysisDto>(It.IsAny<Analysis>()), Times.Never);
    }

    [Fact]
    public async Task GetAnalysesByDataSetAsync_ShouldReturnAnalyses()
    {
        // Arrange
        var analyses = new List<Analysis>
        {
            new() { Id = 1, Name = "Analysis 1" },
            new() { Id = 2, Name = "Analysis 2" }
        };

        var analysisDtos = new List<AnalysisDto>
        {
            new() { Id = 1, Name = "Analysis 1" },
            new() { Id = 2, Name = "Analysis 2" }
        };

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(1)).ReturnsAsync(analyses);
        _mockMapper.Setup(m => m.Map<IEnumerable<AnalysisDto>>(analyses)).Returns(analysisDtos);

        // Act
        var result = await _service.GetAnalysesByDataSetAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        _mockRepository.Verify(r => r.GetByDataSetIdAsync(1), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<AnalysisDto>>(analyses), Times.Once);
    }

    [Fact]
    public async Task GetAnalysisResultAsync_WithExistingAnalysis_ShouldReturnResult()
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Status = AnalysisStatus.Completed,
            Results = JsonSerializer.Serialize(new { test = "data" })
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);

        // Act
        var result = await _service.GetAnalysisResultAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.AnalysisId.Should().Be(1);
        result.Status.Should().Be(AnalysisStatus.Completed);
        result.Results.Should().NotBeNull();
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetAnalysisResultAsync_WithNonExistingAnalysis_ShouldThrowArgumentException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Analysis?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GetAnalysisResultAsync(999));
        
        exception.Message.Should().Contain("Failed to complete GetAnalysisResultAsync for analysis ID 999");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Analysis with ID 999 not found");
    }

    [Fact]
    public async Task DeleteAnalysisAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAnalysisAsync(1);

        // Assert
        result.Should().BeTrue();
        
        _mockRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAnalysisAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAnalysisAsync(999);

        // Assert
        result.Should().BeFalse();
        
        _mockRepository.Verify(r => r.DeleteAsync(999), Times.Once);
    }

    [Fact]
    public async Task RunAnalysisAsync_WithNonExistingAnalysis_ShouldThrowArgumentException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Analysis?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RunAnalysisAsync(999));
        
        exception.Message.Should().Contain("Failed to complete RunAnalysisAsync for analysis ID 999");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Analysis with ID 999 not found");
    }

    [Fact]
    public async Task RunAnalysisAsync_WithProcessingAnalysis_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Status = AnalysisStatus.Processing
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RunAnalysisAsync(1));
        
        exception.Message.Should().Contain("Failed to complete RunAnalysisAsync for analysis ID 1");
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("Analysis with ID 1 is already in progress");
    }

    [Fact]
    public async Task RunAnalysisAsync_WithCompletedAnalysis_ShouldReturnExistingResult()
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Status = AnalysisStatus.Completed
        };

        var analysisDto = new AnalysisDto { Id = 1, Name = "Test Analysis" };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);
        _mockMapper.Setup(m => m.Map<AnalysisDto>(analysis)).Returns(analysisDto);

        // Act
        var result = await _service.RunAnalysisAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockMapper.Verify(m => m.Map<AnalysisDto>(analysis), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Analysis>()), Times.Never);
    }

    [Fact]
    public async Task RunAnalysisAsync_WithValidAnalysis_ShouldExecuteAndUpdate()
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Type = AnalysisType.Statistical,
            Status = AnalysisStatus.Pending
        };

        var updatedAnalysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Status = AnalysisStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        var analysisDto = new AnalysisDto { Id = 1, Name = "Test Analysis" };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Analysis>())).ReturnsAsync(updatedAnalysis);
        _mockMapper.Setup(m => m.Map<AnalysisDto>(updatedAnalysis)).Returns(analysisDto);

        // Act
        var result = await _service.RunAnalysisAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Analysis>()), Times.AtLeast(2)); // Processing and Success states
        _mockMapper.Verify(m => m.Map<AnalysisDto>(updatedAnalysis), Times.Once);
    }

    [Theory]
    [InlineData(AnalysisType.Normalization)]
    [InlineData(AnalysisType.Comparison)]
    [InlineData(AnalysisType.Statistical)]
    [InlineData(AnalysisType.DataCleaning)]
    [InlineData(AnalysisType.OutlierDetection)]
    [InlineData(AnalysisType.CorrelationAnalysis)]
    [InlineData(AnalysisType.TrendAnalysis)]
    [InlineData(AnalysisType.Custom)]
    public async Task RunAnalysisAsync_WithDifferentTypes_ShouldExecuteCorrectAnalysis(AnalysisType analysisType)
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Type = analysisType,
            Status = AnalysisStatus.Pending
        };

        var updatedAnalysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Status = AnalysisStatus.Completed
        };

        var analysisDto = new AnalysisDto { Id = 1, Name = "Test Analysis" };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Analysis>())).ReturnsAsync(updatedAnalysis);
        _mockMapper.Setup(m => m.Map<AnalysisDto>(updatedAnalysis)).Returns(analysisDto);

        // Act
        var result = await _service.RunAnalysisAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Analysis>()), Times.AtLeast(2)); // Processing and Success states
    }

    [Fact]
    public async Task RunAnalysisAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var analysis = new Analysis 
        { 
            Id = 1, 
            Name = "Test Analysis",
            Type = AnalysisType.Statistical,
            Status = AnalysisStatus.Pending
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(analysis);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Analysis>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RunAnalysisAsync(1));
        
        exception.Message.Should().Contain("Failed to complete RunAnalysisAsync for analysis ID 1");
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Be("Database error");
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Analysis>()), Times.AtLeastOnce);
    }
} 