using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Normaize.Data;

public class NormaizeContextFactory : IDesignTimeDbContextFactory<NormaizeContext>
{
    public NormaizeContext CreateDbContext(string[] args)
    {
        // Load .env file for design-time tools
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
            throw new InvalidOperationException($"Could not find .env file. Looked in: {envPath}\n" +
                "Please ensure your .env file exists in the project root directory with MySQL configuration.");
        }
        
        var host = Environment.GetEnvironmentVariable("MYSQLHOST");
        var database = Environment.GetEnvironmentVariable("MYSQLDATABASE");
        var user = Environment.GetEnvironmentVariable("MYSQLUSER");
        var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
        var port = Environment.GetEnvironmentVariable("MYSQLPORT");
        
        // Log the environment variables (without password)
        Console.WriteLine($"Database configuration:");
        Console.WriteLine($"  Host: {host ?? "NOT SET"}");
        Console.WriteLine($"  Database: {database ?? "NOT SET"}");
        Console.WriteLine($"  User: {user ?? "NOT SET"}");
        Console.WriteLine($"  Port: {port ?? "NOT SET"}");
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database environment variables are not set. Please check your .env file contains:\n" +
                "MYSQLHOST=your_host\n" +
                "MYSQLDATABASE=your_database\n" +
                "MYSQLUSER=your_user\n" +
                "MYSQLPASSWORD=your_password\n" +
                "MYSQLPORT=3306");
        }
        
        var connectionString = $"Server={host};Database={database};User={user};Password={password};Port={port};CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;";
        
        Console.WriteLine($"Using MySQL database: {database} on {host}:{port}");
        
        var optionsBuilder = new DbContextOptionsBuilder<NormaizeContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));

        return new NormaizeContext(optionsBuilder.Options);
    }
} 