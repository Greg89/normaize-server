using Microsoft.EntityFrameworkCore;
using Normaize.API.Middleware;
using Normaize.Core.Interfaces;
using Normaize.API.Services;
using Normaize.Data;
using Normaize.Data.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Normaize API", Version = "v1" });
            // c.EnableAnnotations(); // Commented out as it's not available in this version
});

// Database
var connectionString = $"Server={Environment.GetEnvironmentVariable("MYSQLHOST")};Database={Environment.GetEnvironmentVariable("MYSQLDATABASE")};User={Environment.GetEnvironmentVariable("MYSQLUSER")};Password={Environment.GetEnvironmentVariable("MYSQLPASSWORD")};Port={Environment.GetEnvironmentVariable("MYSQLPORT")};";
builder.Services.AddDbContext<NormaizeContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Services
builder.Services.AddScoped<IDataProcessingService, DataProcessingService>();
builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
builder.Services.AddScoped<IDataVisualizationService, DataVisualizationService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Repositories
builder.Services.AddScoped<IDataSetRepository, DataSetRepository>();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// HTTP Client
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Beta", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Global exception handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Run(); 