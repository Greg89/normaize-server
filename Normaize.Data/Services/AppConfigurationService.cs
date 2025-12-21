using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using DotNetEnv;

namespace Normaize.Data.Services;

public class AppConfigurationService : IAppConfigurationService
{
    private readonly ILogger<AppConfigurationService> _logger;
    private bool _environmentVariablesLoaded = false;
    private readonly object _loadLock = new();

    public AppConfigurationService(ILogger<AppConfigurationService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void LoadEnvironmentVariables()
    {
        lock (_loadLock)
        {
            if (_environmentVariablesLoaded)
            {
                _logger.LogDebug("Environment variables already loaded, skipping");
                return;
            }

            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var projectRoot = currentDir;

                while (!File.Exists(Path.Combine(projectRoot, ".env")) && Directory.GetParent(projectRoot) != null)
                {
                    projectRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
                }

                var envPath = Path.Combine(projectRoot, ".env");
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                    _logger.LogInformation("Environment variables loaded from {EnvPath}", envPath);
                }
                else
                {
                    Env.Load(); // Fallback to default behavior
                    _logger.LogInformation("Environment variables loaded using default behavior");
                }

                _environmentVariablesLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading environment variables");
                throw new InvalidOperationException("Failed to load environment variables", ex);
            }
        }
    }

    public string GetEnvironment() =>
        Environment.GetEnvironmentVariable(SharedConstants.Environment.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;

    public string? GetSeqUrl() =>
        Environment.GetEnvironmentVariable("SEQ_URL");

    public string? GetSeqApiKey() =>
        Environment.GetEnvironmentVariable("SEQ_API_KEY");

    public DatabaseConfig GetDatabaseConfig()
    {
        // Prefer standard Postgres env vars; allow generic DB_* fallbacks.
        // Note: docker-compose sets ConnectionStrings__DefaultConnection for the API container,
        // but this method returns structured components (Host/Database/User/Password/Port).
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ??
                   Environment.GetEnvironmentVariable("DB_HOST");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ??
                       Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ??
                   Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ??
                       Environment.GetEnvironmentVariable("DB_PASSWORD");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ??
                   Environment.GetEnvironmentVariable("DB_PORT") ??
                   "5432";

        return new DatabaseConfig
        {
            Host = host,
            Database = database,
            User = user,
            Password = password,
            Port = port
        };
    }

    public bool HasDatabaseConnection()
    {
        // docker-compose uses this, so treat it as a configured DB
        var explicitConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
            Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            _logger.LogDebug("Database connection detected via explicit connection string");
            return true;
        }

        var config = GetDatabaseConfig();
        var hasConnection = !string.IsNullOrEmpty(config.Host) &&
                            !string.IsNullOrEmpty(config.Database) &&
                            !string.IsNullOrEmpty(config.User) &&
                            !string.IsNullOrEmpty(config.Password);

        _logger.LogDebug("Database connection check: Host={Host}, Database={Database}, User={User}, HasConnection={HasConnection}",
            !string.IsNullOrEmpty(config.Host), !string.IsNullOrEmpty(config.Database), !string.IsNullOrEmpty(config.User),
            hasConnection);

        return hasConnection;
    }

    public bool IsProductionLike()
    {
        var environment = GetEnvironment();
        var isProductionLike = environment.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                              environment.Equals("Staging", StringComparison.OrdinalIgnoreCase) ||
                              environment.Equals("Beta", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Production-like environment check: Environment={Environment}, IsProductionLike={IsProductionLike}",
            environment, isProductionLike);

        return isProductionLike;
    }

    public bool IsContainerized()
    {
        var hasPort = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
        var hasDockerEnv = File.Exists("/.dockerenv");
        var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        var isContainerized = hasPort || hasDockerEnv || isRunningInContainer;

        _logger.LogDebug("Container check: HasPort={HasPort}, HasDockerEnv={HasDockerEnv}, IsRunningInContainer={IsRunningInContainer}, IsContainerized={IsContainerized}",
            hasPort, hasDockerEnv, isRunningInContainer, isContainerized);

        return isContainerized;
    }

    public string GetPort() =>
        Environment.GetEnvironmentVariable("PORT") ?? "5000";

    public string? GetHttpsPort() =>
        Environment.GetEnvironmentVariable("HTTPS_PORT");
}