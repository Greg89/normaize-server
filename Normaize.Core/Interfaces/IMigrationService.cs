namespace Normaize.Core.Interfaces;

public interface IMigrationService
{
    Task<MigrationResult> ApplyMigrationsAsync();
    Task<MigrationResult> VerifySchemaAsync();
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> PendingMigrations { get; set; } = new();
    public List<string> MissingColumns { get; set; } = new();
    public string? ErrorMessage { get; set; }
} 