using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Constants;
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
        environment.Should().BeOneOf(AppConstants.Environment.DEVELOPMENT, AppConstants.Environment.STAGING, AppConstants.Environment.PRODUCTION, AppConstants.Environment.TEST);
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
        config.Port.Should().Be(AppConstants.Database.DEFAULT_PORT); // Default value
    }



    [Fact]
    public void GetPort_ShouldReturnValidPort()
    {
        // Act
        var port = _service.GetPort();

        // Assert
        port.Should().NotBeNullOrEmpty();
        port.Should().MatchRegex(AppConstants.Validation.NUMERIC_ONLY_PATTERN); // Should be numeric
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
            Host = AppConstants.Database.DEFAULT_HOST,
            Database = AppConstants.Database.DEFAULT_DATABASE,
            User = AppConstants.Database.DEFAULT_USER,
            Password = AppConstants.Database.DEFAULT_PASSWORD,
            Port = AppConstants.Database.DEFAULT_PORT
        };

        // Assert
        config.Host.Should().Be(AppConstants.Database.DEFAULT_HOST);
        config.Database.Should().Be(AppConstants.Database.DEFAULT_DATABASE);
        config.User.Should().Be(AppConstants.Database.DEFAULT_USER);
        config.Password.Should().Be(AppConstants.Database.DEFAULT_PASSWORD);
        config.Port.Should().Be(AppConstants.Database.DEFAULT_PORT);
    }

    [Fact]
    public void DatabaseConfig_ToConnectionString_ShouldReturnValidConnectionString()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Host = AppConstants.Database.DEFAULT_HOST,
            Database = AppConstants.Database.DEFAULT_DATABASE,
            User = AppConstants.Database.DEFAULT_USER,
            Password = AppConstants.Database.DEFAULT_PASSWORD,
            Port = AppConstants.Database.DEFAULT_PORT
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain($"{AppConstants.Database.SERVER_PREFIX}{AppConstants.Database.DEFAULT_HOST}");
        connectionString.Should().Contain($"{AppConstants.Database.DATABASE_PREFIX}{AppConstants.Database.DEFAULT_DATABASE}");
        connectionString.Should().Contain($"{AppConstants.Database.USER_PREFIX}{AppConstants.Database.DEFAULT_USER}");
        connectionString.Should().Contain($"{AppConstants.Database.PASSWORD_PREFIX}{AppConstants.Database.DEFAULT_PASSWORD}");
        connectionString.Should().Contain($"{AppConstants.Database.PORT_PREFIX}{AppConstants.Database.DEFAULT_PORT}");
        connectionString.Should().Contain($"{AppConstants.Database.CHARSET_PREFIX}{AppConstants.Database.DEFAULT_CHARSET}");
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
            Port = AppConstants.Database.DEFAULT_PORT
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain(AppConstants.Database.SERVER_PREFIX);
        connectionString.Should().Contain(AppConstants.Database.DATABASE_PREFIX);
        connectionString.Should().Contain(AppConstants.Database.USER_PREFIX);
        connectionString.Should().Contain(AppConstants.Database.PASSWORD_PREFIX);
        connectionString.Should().Contain($"{AppConstants.Database.PORT_PREFIX}{AppConstants.Database.DEFAULT_PORT}");
    }
}