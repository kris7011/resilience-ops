namespace ResilienceOps.Api.Features.Incidents;

public interface IIncidentRepository
{
    Task<IReadOnlyCollection<Incident>>
        GetByRiskIdAsync(
            Guid riskId,
            CancellationToken cancellationToken);

    Task<Incident?> GetByIdAsync(
        Guid riskId,
        Guid incidentId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken);

    Task<IncidentStatusUpdateResult> UpdateStatusAsync(
        Guid riskId,
        Guid incidentId,
        IncidentStatus status,
        CancellationToken cancellationToken);
}

public enum IncidentStatusUpdateOutcome
{
    Updated,
    NotFound,
    InvalidTransition,
    Conflict
}

public sealed record IncidentStatusUpdateResult(
    IncidentStatusUpdateOutcome Outcome,
    Incident? Incident,
    string? Error);
