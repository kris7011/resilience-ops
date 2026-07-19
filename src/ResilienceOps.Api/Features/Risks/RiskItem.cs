namespace ResilienceOps.Api.Features.Risks;

public sealed record RiskItem(
    Guid Id,
    string Title,
    string Description,
    RiskSeverity Severity,
    RiskStatus Status,
    DateTimeOffset CreatedUtc)
{
    public static RiskItem Create(
        string title,
        string description,
        RiskSeverity severity)
    {
        return new RiskItem(
            Id: Guid.NewGuid(),
            Title: title.Trim(),
            Description: description.Trim(),
            Severity: severity,
            Status: RiskStatus.Open,
            CreatedUtc: DateTimeOffset.UtcNow);
    }
}