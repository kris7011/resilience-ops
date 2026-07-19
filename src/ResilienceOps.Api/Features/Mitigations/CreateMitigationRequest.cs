namespace ResilienceOps.Api.Features.Mitigations;

public sealed record CreateMitigationRequest(
    string? Description,
    string? Owner,
    DateTimeOffset DueDateUtc);