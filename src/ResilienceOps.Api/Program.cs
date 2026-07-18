using ResilienceOps.Api.Contracts;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

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