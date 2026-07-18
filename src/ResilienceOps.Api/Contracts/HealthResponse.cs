namespace ResilienceOps.Api.Contracts;

public sealed record HealthResponse(
    string Status,
    string Service,
    string Environment,
    DateTimeOffset TimestampUtc);