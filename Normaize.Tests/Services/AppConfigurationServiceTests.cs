using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Interfaces;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class AppConfigurationServiceTests
{
    private readonly AppConfigurationService _service;

    public AppConfigurationServiceTests()
    {
        var mockLogger = new Mock<ILogger<AppConfigurationService>>();
        _service = new AppConfigurationService(mockLogger.Object);
    }

    [Fact]
    public void IAppConfigurationService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IAppConfigurationService);

        // Assert
        interfaceType.GetMethod("LoadEnvironmentVariables")!.Should().NotBeNull();
        interfaceType.GetMethod("GetEnvironment")!.Should().NotBeNull();
        interfaceType.GetMethod("GetSeqUrl")!.Should().NotBeNull();
        interfaceType.GetMethod("GetSeqApiKey")!.Should().NotBeNull();
        interfaceType.GetMethod("GetDatabaseConfig")!.Should().NotBeNull();
        interfaceType.GetMethod("HasDatabaseConnection")!.Should().NotBeNull();
        interfaceType.GetMethod("IsProductionLike")!.Should().NotBeNull();
        interfaceType.GetMethod("IsContainerized")!.Should().NotBeNull();
        interfaceType.GetMethod("GetPort")!.Should().NotBeNull();
        interfaceType.GetMethod("GetHttpsPort")!.Should().NotBeNull();
    }

    [Fact]
    public void IAppConfigurationService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IAppConfigurationService);

        // Assert
        interfaceType.GetMethod("LoadEnvironmentVariables")!.ReturnType.Should().Be(typeof(void));
        interfaceType.GetMethod("GetEnvironment")!.ReturnType.Should().Be<string>();
        interfaceType.GetMethod("GetSeqUrl")!.ReturnType.Should().Be<string?>();
        interfaceType.GetMethod("GetSeqApiKey")!.ReturnType.Should().Be<string?>();
        interfaceType.GetMethod("GetDatabaseConfig")!.ReturnType.Should().Be<DatabaseConfig>();
        interfaceType.GetMethod("HasDatabaseConnection")!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("IsProductionLike")!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("IsContainerized")!.ReturnType.Should().Be<bool>();
        interfaceType.GetMethod("GetPort")!.ReturnType.Should().Be<string>();
        interfaceType.GetMethod("GetHttpsPort")!.ReturnType.Should().Be<string?>();
    }

    [Fact]
    public void IAppConfigurationService_ShouldBePublic()
    {
        // Assert
        typeof(IAppConfigurationService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IAppConfigurationService_ShouldBeInterface()
    {
        // Assert
        typeof(IAppConfigurationService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void LoadEnvironmentVariables_ShouldNotThrow()
    {
        // Act & Assert
        var action = _service.LoadEnvironmentVariables;
        action.Should().NotThrow();
    }

    [Fact]
    public void GetEnvironment_ShouldReturnValidEnvironment()
    {
        // Act
        var environment = _service.GetEnvironment();

        // Assert
        environment.Should().NotBeNullOrEmpty();
        environment.Should().BeOneOf("Development", "Staging", "Production", "Test");
    }

    [Fact]
    public void GetSeqUrl_ShouldReturnStringOrNull()
    {
        // Act
        var seqUrl = _service.GetSeqUrl();

        // Assert
        seqUrl.Should().BeNullOrEmpty(); // Accept null or empty as valid
    }

    [Fact]
    public void GetSeqApiKey_ShouldReturnStringOrNull()
    {
        // Act
        var seqApiKey = _service.GetSeqApiKey();

        // Assert
        seqApiKey.Should().BeNullOrEmpty(); // Accept null or empty as valid
    }

    [Fact]
    public void GetDatabaseConfig_ShouldReturnValidConfig()
    {
        // Act
        var config = _service.GetDatabaseConfig();

        // Assert
        config.Should().NotBeNull();
        // These can be null if not configured in the environment
        // config.Host, config.Database, config.User, config.Password can be null
        config.Port.Should().Be("3306"); // Default value
    }



    [Fact]
    public void GetPort_ShouldReturnValidPort()
    {
        // Act
        var port = _service.GetPort();

        // Assert
        port.Should().NotBeNullOrEmpty();
        port.Should().MatchRegex(@"^\d+$"); // Should be numeric
    }

    [Fact]
    public void GetHttpsPort_ShouldReturnStringOrNull()
    {
        // Act
        var httpsPort = _service.GetHttpsPort();

        // Assert
        httpsPort.Should().BeNullOrEmpty(); // Accept null or empty as valid
    }

    [Fact]
    public void DatabaseConfig_ShouldHaveRequiredProperties()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Host = "localhost",
            Database = "testdb",
            User = "testuser",
            Password = "testpass",
            Port = "3306"
        };

        // Assert
        config.Host.Should().Be("localhost");
        config.Database.Should().Be("testdb");
        config.User.Should().Be("testuser");
        config.Password.Should().Be("testpass");
        config.Port.Should().Be("3306");
    }

    [Fact]
    public void DatabaseConfig_ToConnectionString_ShouldReturnValidConnectionString()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Host = "localhost",
            Database = "testdb",
            User = "testuser",
            Password = "testpass",
            Port = "3306"
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Server=localhost");
        connectionString.Should().Contain("Database=testdb");
        connectionString.Should().Contain("User=testuser");
        connectionString.Should().Contain("Password=testpass");
        connectionString.Should().Contain("Port=3306");
        connectionString.Should().Contain("CharSet=utf8mb4");
    }

    [Fact]
    public void DatabaseConfig_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Host = null,
            Database = null,
            User = null,
            Password = null,
            Port = "3306"
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Server=");
        connectionString.Should().Contain("Database=");
        connectionString.Should().Contain("User=");
        connectionString.Should().Contain("Password=");
        connectionString.Should().Contain("Port=3306");
    }
}