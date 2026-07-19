namespace ResilienceOps.Api.Features.Risks;

public sealed class RiskItem
{
    private RiskItem()
    {
    }

    private RiskItem(
        Guid id,
        string title,
        string description,
        RiskSeverity severity,
        RiskStatus status,
        DateTimeOffset createdUtc)
    {
        Id = id;
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } =
        string.Empty;

    public string Description { get; private set; } =
        string.Empty;

    public RiskSeverity Severity { get; private set; }

    public RiskStatus Status { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public static RiskItem Create(
        string title,
        string description,
        RiskSeverity severity)
    {
        return new RiskItem(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: description.Trim(),
            severity: severity,
            status: RiskStatus.Open,
            createdUtc: DateTimeOffset.UtcNow);
    }

    public bool ChangeStatus(
        RiskStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(newStatus),
                newStatus,
                "The risk status is invalid.");
        }

        if (Status == newStatus)
        {
            return false;
        }

        Status = newStatus;

        return true;
    }
}