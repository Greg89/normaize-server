using Normaize.API.Configuration;
using Normaize.Core.Interfaces;
using Serilog;
using Serilog.Events;

// Create a basic logger for initial configuration
var initialLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Load environment variables from .env file
var appConfigService = new Normaize.Data.Services.AppConfigurationService(
    new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<Normaize.Data.Services.AppConfigurationService>());
appConfigService.LoadEnvironmentVariables();

// Configure Serilog
var environment = appConfigService.GetEnvironment();
var seqUrl = appConfigService.GetSeqUrl();

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
        apiKey: appConfigService.GetSeqApiKey());
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for dependency injection
builder.Host.UseSerilog();

// Configure all services
ServiceConfiguration.ConfigureServices(builder);

var app = builder.Build();

// Configure startup (migrations, health checks)
var startupService = app.Services.GetRequiredService<IStartupService>();
await startupService.ConfigureStartupAsync();

// Configure middleware pipeline
MiddlewareConfiguration.ConfigureMiddleware(app);

// Use PORT environment variable if set (for Railway)
var port = appConfigService.GetPort();
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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "Required for ASP.NET Core integration testing")]
public partial class Program { }
 