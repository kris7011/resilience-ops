namespace ResilienceOps.Api.Features.Mitigations;

public interface IMitigationRepository
{
    Task<IReadOnlyCollection<MitigationAction>>
        GetByRiskIdAsync(
            Guid riskId,
            CancellationToken cancellationToken);

    Task<MitigationAction?> GetByIdAsync(
        Guid riskId,
        Guid mitigationId,
        CancellationToken cancellationToken);

    Task AddAsync(
        MitigationAction mitigation,
        CancellationToken cancellationToken);
}