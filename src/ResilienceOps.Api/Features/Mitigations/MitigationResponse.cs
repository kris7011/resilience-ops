namespace ResilienceOps.Api.Features.Mitigations;

public sealed record MitigationResponse(
    Guid Id,
    Guid RiskId,
    string Description,
    string Owner,
    DateTimeOffset DueDateUtc,
    MitigationStatus Status,
    DateTimeOffset CreatedUtc)
{
    public static MitigationResponse FromMitigation(
        MitigationAction mitigation)
    {
        return new MitigationResponse(
            Id: mitigation.Id,
            RiskId: mitigation.RiskId,
            Description: mitigation.Description,
            Owner: mitigation.Owner,
            DueDateUtc: mitigation.DueDateUtc,
            Status: mitigation.Status,
            CreatedUtc: mitigation.CreatedUtc);
    }
}