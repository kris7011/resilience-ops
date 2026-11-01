namespace ResilienceOps.Api.Features.Incidents;

public sealed record CreateIncidentRequest(
    string? Title,
    string? Description,
    string? ResponsiblePerson,
    IReadOnlyCollection<Guid>? MitigationIds);
