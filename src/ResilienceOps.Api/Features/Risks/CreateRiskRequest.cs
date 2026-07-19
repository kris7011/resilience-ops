namespace ResilienceOps.Api.Features.Risks;

public sealed record CreateRiskRequest(
    string? Title,
    string? Description,
    RiskSeverity Severity);