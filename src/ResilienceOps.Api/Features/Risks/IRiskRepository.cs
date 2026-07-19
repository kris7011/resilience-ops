namespace ResilienceOps.Api.Features.Risks;

public interface IRiskRepository
{
    Task<IReadOnlyCollection<RiskItem>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<RiskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        RiskItem risk,
        CancellationToken cancellationToken);

    Task<RiskItem?> UpdateStatusAsync(
        Guid id,
        RiskStatus status,
        CancellationToken cancellationToken);
}