namespace Normaize.Core.Interfaces;

public interface IStartupService
{
    /// <summary>
    /// Configures and runs startup checks including database migrations and health checks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the startup operation</returns>
    Task ConfigureStartupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether startup checks should be run based on environment and configuration
    /// </summary>
    /// <returns>True if startup checks should be run, false otherwise</returns>
    bool ShouldRunStartupChecks();

    /// <summary>
    /// Applies database migrations with retry logic
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the migration operation</returns>
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs startup health checks with retry logic
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the health check operation</returns>
    Task PerformHealthChecksAsync(CancellationToken cancellationToken = default);
}