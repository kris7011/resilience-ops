using ResilienceOps.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet(
    "/api/health",
    (IHostEnvironment environment) =>
    {
        var response = new HealthResponse(
            Status: "Healthy",
            Service: "ResilienceOps.Api",
            Environment: environment.EnvironmentName,
            TimestampUtc: DateTimeOffset.UtcNow);

        return Results.Ok(response);
    });

app.Run();