using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service for environment detection and configuration queries.
/// Replaces legacy IAppConfigurationService with modern ASP.NET Core approach.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Checks if the current environment is production-like (Production, Staging, Beta).
    /// </summary>
    bool IsProductionLike();

    /// <summary>
    /// Checks if the application is running in a container (Docker, Kubernetes).
    /// </summary>
    bool IsContainerized();

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    string GetEnvironmentName();
}

/// <summary>
/// Implementation of environment detection service using ASP.NET Core conventions.
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<EnvironmentService> _logger;

    public EnvironmentService(
        IHostEnvironment hostEnvironment,
        ILogger<EnvironmentService> logger)
    {
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the current environment is production-like.
    /// </summary>
    /// <remarks>
    /// Production-like environments include: Production, Staging, Beta.
    /// These environments require stricter validation and error handling.
    /// </remarks>
    public bool IsProductionLike()
    {
        var environmentName = _hostEnvironment.EnvironmentName;
        var isProductionLike = environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                              environmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase) ||
                              environmentName.Equals("Beta", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Production-like environment check: Environment={Environment}, IsProductionLike={IsProductionLike}",
            environmentName, isProductionLike);

        return isProductionLike;
    }

    /// <summary>
    /// Checks if the application is running in a container.
    /// </summary>
    /// <remarks>
    /// Detection methods:
    /// 1. PORT environment variable (common in container platforms like Railway, Heroku, Cloud Run)
    /// 2. /.dockerenv file existence (Docker containers)
    /// 3. DOTNET_RUNNING_IN_CONTAINER environment variable (official .NET detection)
    /// </remarks>
    public bool IsContainerized()
    {
        var hasPort = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
        var hasDockerEnv = File.Exists("/.dockerenv");
        var isDotNetContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        var isContainerized = hasPort || hasDockerEnv || isDotNetContainer;

        _logger.LogDebug("Container check: HasPort={HasPort}, HasDockerEnv={HasDockerEnv}, IsDotNetContainer={IsDotNetContainer}, IsContainerized={IsContainerized}",
            hasPort, hasDockerEnv, isDotNetContainer, isContainerized);

        return isContainerized;
    }

    /// <summary>
    /// Gets the current environment name (Development, Production, Staging, etc.).
    /// </summary>
    public string GetEnvironmentName()
    {
        return _hostEnvironment.EnvironmentName;
    }
}
