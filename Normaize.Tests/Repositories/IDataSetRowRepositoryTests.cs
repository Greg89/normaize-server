using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Repositories;

public class IDataSetRowRepositoryTests
{
    [Fact]
    public void IDataSetRowRepository_ShouldDefineRequiredMethods()
    {
        // This test ensures the interface defines all required methods
        // It's a contract test to verify the interface is complete
        
        // Arrange & Act - We're testing the interface definition, not implementation
        var interfaceType = typeof(IDataSetRowRepository);
        
        // Assert - Verify all required methods exist
        interfaceType.GetMethod("GetByDataSetIdAsync", new[] { typeof(int) })
            .Should().NotBeNull("GetByDataSetIdAsync(int) method should exist");
            
        interfaceType.GetMethod("GetByDataSetIdAsync", new[] { typeof(int), typeof(int), typeof(int) })
            .Should().NotBeNull("GetByDataSetIdAsync(int, int, int) method should exist");
            
        interfaceType.GetMethod("GetByIdAsync", new[] { typeof(int) })
            .Should().NotBeNull("GetByIdAsync method should exist");
            
        interfaceType.GetMethod("AddAsync", new[] { typeof(DataSetRow) })
            .Should().NotBeNull("AddAsync method should exist");
            
        interfaceType.GetMethod("AddRangeAsync", new[] { typeof(IEnumerable<DataSetRow>) })
            .Should().NotBeNull("AddRangeAsync method should exist");
            
        interfaceType.GetMethod("DeleteAsync", new[] { typeof(int) })
            .Should().NotBeNull("DeleteAsync method should exist");
            
        interfaceType.GetMethod("DeleteByDataSetIdAsync", new[] { typeof(int) })
            .Should().NotBeNull("DeleteByDataSetIdAsync method should exist");
            
        interfaceType.GetMethod("GetCountByDataSetIdAsync", new[] { typeof(int) })
            .Should().NotBeNull("GetCountByDataSetIdAsync method should exist");
    }

    [Fact]
    public void IDataSetRowRepository_MethodsShouldReturnCorrectTypes()
    {
        // This test verifies that the interface methods have the correct return types
        
        var interfaceType = typeof(IDataSetRowRepository);
        
        // Verify return types
        interfaceType.GetMethod("GetByDataSetIdAsync", new[] { typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<IEnumerable<DataSetRow>>));
            
        interfaceType.GetMethod("GetByDataSetIdAsync", new[] { typeof(int), typeof(int), typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<IEnumerable<DataSetRow>>));
            
        interfaceType.GetMethod("GetByIdAsync", new[] { typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<DataSetRow?>));
            
        interfaceType.GetMethod("AddAsync", new[] { typeof(DataSetRow) })!
            .ReturnType.Should().Be(typeof(Task<DataSetRow>));
            
        interfaceType.GetMethod("AddRangeAsync", new[] { typeof(IEnumerable<DataSetRow>) })!
            .ReturnType.Should().Be(typeof(Task<IEnumerable<DataSetRow>>));
            
        interfaceType.GetMethod("DeleteAsync", new[] { typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<bool>));
            
        interfaceType.GetMethod("DeleteByDataSetIdAsync", new[] { typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<bool>));
            
        interfaceType.GetMethod("GetCountByDataSetIdAsync", new[] { typeof(int) })!
            .ReturnType.Should().Be(typeof(Task<int>));
    }

    [Fact]
    public void IDataSetRowRepository_ShouldBePublic()
    {
        // Verify the interface is public and accessible
        typeof(IDataSetRowRepository).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IDataSetRowRepository_ShouldBeInterface()
    {
        // Verify it's actually an interface
        typeof(IDataSetRowRepository).IsInterface.Should().BeTrue();
    }
} 