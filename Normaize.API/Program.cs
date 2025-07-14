using Normaize.API.Configuration;
using Serilog;
using Serilog.Events;

// Load environment variables from .env file
AppConfiguration.LoadEnvironmentVariables();

// Configure Serilog
var environment = AppConfiguration.GetEnvironment();
var seqUrl = AppConfiguration.GetSeqUrl();

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information
    );

// Add Seq sink for non-local environments
if (!string.IsNullOrEmpty(seqUrl) && environment != "Development")
{
    loggerConfiguration.WriteTo.Seq(seqUrl, 
        restrictedToMinimumLevel: LogEventLevel.Information,
        apiKey: AppConfiguration.GetSeqApiKey());
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for dependency injection
builder.Host.UseSerilog();

// Configure all services
ServiceConfiguration.ConfigureServices(builder);

var app = builder.Build();

// Configure startup (migrations, health checks)
await StartupConfiguration.ConfigureStartup(app);

// Configure middleware pipeline
MiddlewareConfiguration.ConfigureMiddleware(app);

// Use PORT environment variable if set (for Railway)
var port = AppConfiguration.GetPort();
app.Urls.Add($"http://*:{port}");

try
{
    Log.Information("Starting Normaize API on port {Port}", port);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Entry point for integration tests
public partial class Program { }
 