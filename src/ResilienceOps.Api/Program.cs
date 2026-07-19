using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ResilienceOps.Api.Contracts;
using ResilienceOps.Api.Data;
using ResilienceOps.Api.Features.Mitigations;
using ResilienceOps.Api.Features.Risks;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var serviceName =
    builder.Configuration["OpenTelemetry:ServiceName"]
    ?? "ResilienceOps.Api";

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(resource =>
        {
            resource.AddService(
                serviceName: serviceName);
        })
        .WithTracing(tracing =>
        {
            tracing.AddSource(
                RiskTelemetry.ActivitySourceName,
                MitigationTelemetry.ActivitySourceName);
        })
        .UseAzureMonitor(options =>
        {
            options.ConnectionString =
                applicationInsightsConnectionString;
        });
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var databaseDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "Data");

Directory.CreateDirectory(databaseDirectory);

var databasePath = Path.Combine(
    databaseDirectory,
    "resilience-ops.db");

builder.Services.AddDbContext<
    ResilienceOpsDbContext>(
    options =>
    {
        options.UseSqlite(
            $"Data Source={databasePath}");
    });

builder.Services.AddScoped<
    IRiskRepository,
    EfCoreRiskRepository>();

builder.Services.AddScoped<
    IMitigationRepository,
    EfCoreMitigationRepository>();

var app = builder.Build();

var startupLogger =
    app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("ResilienceOps.Startup");

if (string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    startupLogger.LogInformation(
        "Azure Monitor telemetry is disabled because no Application Insights connection string is configured");
}
else
{
    startupLogger.LogInformation(
        "Azure Monitor telemetry is enabled for service {ServiceName}",
        serviceName);
}

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                ResilienceOpsDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseCors(FrontendCorsPolicy);

app.MapGet(
    "/api/health",
    (IHostEnvironment environment) =>
    {
        var response = new HealthResponse(
            Status: "Healthy",
            Service: serviceName,
            Environment:
                environment.EnvironmentName,
            TimestampUtc:
                DateTimeOffset.UtcNow);

        return Results.Ok(response);
    });

app.MapRiskEndpoints();
app.MapMitigationEndpoints();

app.Run();

public partial class Program
{
}