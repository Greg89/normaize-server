using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Normaize.Data;

public class NormaizeContextFactory : IDesignTimeDbContextFactory<NormaizeContext>
{
    public NormaizeContext CreateDbContext(string[] args)
    {
        // Try to load .env file for design-time tools (local development)
        var currentDir = Directory.GetCurrentDirectory();

        // Find the project root directory (where .env file is located)
        var projectRoot = currentDir;
        while (!File.Exists(Path.Combine(projectRoot, ".env")) && Directory.GetParent(projectRoot) != null)
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        var envPath = Path.Combine(projectRoot, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Console.WriteLine($"Loaded .env file from: {envPath}");
        }
        else
        {
            Console.WriteLine($"No .env file found at: {envPath}");
            Console.WriteLine("Using environment variables directly (CI/CD environment)");
        }

        // Prefer an explicit connection string (docker-compose / CI)
        var explicitConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
            Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            Console.WriteLine("Using explicit database connection string from environment");
            var optionsBuilder1 = new DbContextOptionsBuilder<NormaizeContext>();
            optionsBuilder1.UseNpgsql(explicitConnectionString);
            return new NormaizeContext(optionsBuilder1.Options);
        }

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB");
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");

        // Log the environment variables (without password)
        Console.WriteLine($"Database configuration:");
        Console.WriteLine($"  Host: {host ?? "NOT SET"}");
        Console.WriteLine($"  Database: {database ?? "NOT SET"}");
        Console.WriteLine($"  User: {user ?? "NOT SET"}");
        Console.WriteLine($"  Port: {port ?? "NOT SET"}");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database environment variables are not set. Please check your .env file or environment variables contain:\n" +
                "POSTGRES_HOST=your_host\n" +
                "POSTGRES_DB=your_database\n" +
                "POSTGRES_USER=your_user\n" +
                "POSTGRES_PASSWORD=your_password\n" +
                "POSTGRES_PORT=5432");
        }

        var connectionString = $"Host={host};Port={port ?? "5432"};Database={database};Username={user};Password={password};";

        Console.WriteLine($"Using Postgres database: {database} on {host}:{port ?? "5432"}");

        var optionsBuilder = new DbContextOptionsBuilder<NormaizeContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NormaizeContext(optionsBuilder.Options);
    }
}