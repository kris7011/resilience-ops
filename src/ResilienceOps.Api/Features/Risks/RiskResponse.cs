namespace ResilienceOps.Api.Features.Risks;

public sealed record RiskResponse(
    Guid Id,
    string Title,
    string Description,
    RiskSeverity Severity,
    RiskStatus Status,
    DateTimeOffset CreatedUtc)
{
    public static RiskResponse FromRisk(RiskItem risk)
    {
        return new RiskResponse(
            Id: risk.Id,
            Title: risk.Title,
            Description: risk.Description,
            Severity: risk.Severity,
            Status: risk.Status,
            CreatedUtc: risk.CreatedUtc);
    }
}