using Normaize.API.Configuration;
using Normaize.Core.Constants;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Configuration;

public class AppConfigurationTests
{
    [Fact]
    public void GetEnvironment_WhenNotSet_ShouldReturnDevelopment()
    {
        // Arrange
        Environment.SetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT, null);

        // Act
        var result = AppConfiguration.GetEnvironment();

        // Assert
        result.Should().Be(AppConstants.Environment.DEVELOPMENT);
    }

    [Fact]
    public void GetEnvironment_WhenSet_ShouldReturnSetValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT, "Production");

        // Act
        var result = AppConfiguration.GetEnvironment();

        // Assert
        result.Should().Be("Production");

        // Cleanup
        Environment.SetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT, null);
    }

    [Fact]
    public void GetDatabaseConfig_WhenNoEnvVars_ShouldReturnNullValues()
    {
        // Arrange
        ClearDatabaseEnvironmentVariables();

        // Act
        var result = AppConfiguration.GetDatabaseConfig();

        // Assert
        result.Host.Should().BeNull();
        result.Database.Should().BeNull();
        result.User.Should().BeNull();
        result.Password.Should().BeNull();
        result.Port.Should().Be("3306");
    }

    [Fact]
    public void GetDatabaseConfig_WhenEnvVarsSet_ShouldReturnCorrectValues()
    {
        // Arrange
        SetDatabaseEnvironmentVariables("testhost", "testdb", "testuser", "testpass", "3307");

        // Act
        var result = AppConfiguration.GetDatabaseConfig();

        // Assert
        result.Host.Should().Be("testhost");
        result.Database.Should().Be("testdb");
        result.User.Should().Be("testuser");
        result.Password.Should().Be("testpass");
        result.Port.Should().Be("3307");

        // Cleanup
        ClearDatabaseEnvironmentVariables();
    }

    [Fact]
    public void GetDatabaseConfig_ShouldPrioritizeMYSQLHOST_OverOtherVariants()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MYSQLHOST", "primary");
        Environment.SetEnvironmentVariable("MYSQL_HOST", "secondary");
        Environment.SetEnvironmentVariable("DB_HOST", "tertiary");

        // Act
        var result = AppConfiguration.GetDatabaseConfig();

        // Assert
        result.Host.Should().Be("primary");

        // Cleanup
        ClearDatabaseEnvironmentVariables();
    }

    [Fact]
    public void HasDatabaseConnection_WhenAllRequiredVarsSet_ShouldReturnTrue()
    {
        // Arrange
        SetDatabaseEnvironmentVariables("host", "db", "user", "pass", "3306");

        // Act
        var result = AppConfiguration.HasDatabaseConnection();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        ClearDatabaseEnvironmentVariables();
    }

    [Fact]
    public void HasDatabaseConnection_WhenMissingRequiredVars_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MYSQLHOST", "host");
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "db");
        // Missing user and password

        // Act
        var result = AppConfiguration.HasDatabaseConnection();

        // Assert
        result.Should().BeFalse();

        // Cleanup
        ClearDatabaseEnvironmentVariables();
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Beta", true)]
    [InlineData("Development", false)]
    [InlineData("Test", false)]
    public void IsProductionLike_ShouldReturnCorrectValue(string environment, bool expected)
    {
        // Arrange
        Environment.SetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT, environment);

        // Act
        var result = AppConfiguration.IsProductionLike();

        // Assert
        result.Should().Be(expected);

        // Cleanup
        Environment.SetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT, null);
    }

    [Fact]
    public void IsContainerized_WhenPortSet_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PORT", "8080");

        // Act
        var result = AppConfiguration.IsContainerized();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        Environment.SetEnvironmentVariable("PORT", null);
    }

    [Fact]
    public void IsContainerized_WhenDockerEnvExists_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

        // Act
        var result = AppConfiguration.IsContainerized();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
    }

    [Fact]
    public void GetPort_WhenNotSet_ShouldReturnDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PORT", null);

        // Act
        var result = AppConfiguration.GetPort();

        // Assert
        result.Should().Be("5000");
    }

    [Fact]
    public void GetPort_WhenSet_ShouldReturnSetValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PORT", "8080");

        // Act
        var result = AppConfiguration.GetPort();

        // Assert
        result.Should().Be("8080");

        // Cleanup
        Environment.SetEnvironmentVariable("PORT", null);
    }

    [Fact]
    public void DatabaseConfig_ToConnectionString_ShouldReturnValidConnectionString()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Host = "testhost",
            Database = "testdb",
            User = "testuser",
            Password = "testpass",
            Port = "3306"
        };

        // Act
        var result = config.ToConnectionString();

        // Assert
        result.Should().Contain("Server=testhost");
        result.Should().Contain("Database=testdb");
        result.Should().Contain("User=testuser");
        result.Should().Contain("Password=testpass");
        result.Should().Contain("Port=3306");
    }

    private static void SetDatabaseEnvironmentVariables(string host, string database, string user, string password, string port)
    {
        Environment.SetEnvironmentVariable("MYSQLHOST", host);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", database);
        Environment.SetEnvironmentVariable("MYSQLUSER", user);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", password);
        Environment.SetEnvironmentVariable("MYSQLPORT", port);
    }

    private static void ClearDatabaseEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
        Environment.SetEnvironmentVariable("MYSQL_HOST", null);
        Environment.SetEnvironmentVariable("MYSQL_DATABASE", null);
        Environment.SetEnvironmentVariable("MYSQL_USER", null);
        Environment.SetEnvironmentVariable("MYSQL_PASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQL_PORT", null);
        Environment.SetEnvironmentVariable("DB_HOST", null);
        Environment.SetEnvironmentVariable("DB_NAME", null);
        Environment.SetEnvironmentVariable("DB_USER", null);
        Environment.SetEnvironmentVariable("DB_PASSWORD", null);
        Environment.SetEnvironmentVariable("DB_PORT", null);
    }
}