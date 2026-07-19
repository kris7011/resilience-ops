using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Contracts;
using ResilienceOps.Api.Data;
using ResilienceOps.Api.Features.Risks;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

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
            Service: "ResilienceOps.Api",
            Environment:
                environment.EnvironmentName,
            TimestampUtc:
                DateTimeOffset.UtcNow);

        return Results.Ok(response);
    });

app.MapRiskEndpoints();

app.Run();