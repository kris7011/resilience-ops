namespace ResilienceOps.Api.Features.Risks;

public interface IRiskRepository
{
    IReadOnlyCollection<RiskItem> GetAll();

    RiskItem? GetById(Guid id);

    RiskItem Add(
        string title,
        string description,
        RiskSeverity severity);
}