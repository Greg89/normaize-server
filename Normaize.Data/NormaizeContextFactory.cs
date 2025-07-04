using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Normaize.Data;

public class NormaizeContextFactory : IDesignTimeDbContextFactory<NormaizeContext>
{
    public NormaizeContext CreateDbContext(string[] args)
    {
        // Load .env file for design-time tools
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        else
        {
            // Try alternative paths
            var altPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(altPath))
            {
                Env.Load(altPath);
            }
        }
        
        var host = Environment.GetEnvironmentVariable("MYSQLHOST");
        var database = Environment.GetEnvironmentVariable("MYSQLDATABASE");
        var user = Environment.GetEnvironmentVariable("MYSQLUSER");
        var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
        var port = Environment.GetEnvironmentVariable("MYSQLPORT");
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Database environment variables are not set. Please check your .env file.");
        }
        
        var connectionString = $"Server={host};Database={database};User={user};Password={password};Port={port};";
        
        var optionsBuilder = new DbContextOptionsBuilder<NormaizeContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));

        return new NormaizeContext(optionsBuilder.Options);
    }
} 