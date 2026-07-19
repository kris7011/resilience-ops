namespace ResilienceOps.Api.Features.Mitigations;

public sealed class MitigationAction
{
    private MitigationAction()
    {
    }

    private MitigationAction(
        Guid id,
        Guid riskId,
        string description,
        string owner,
        DateTimeOffset dueDateUtc,
        MitigationStatus status,
        DateTimeOffset createdUtc)
    {
        Id = id;
        RiskId = riskId;
        Description = description;
        Owner = owner;
        DueDateUtc = dueDateUtc;
        Status = status;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public Guid RiskId { get; private set; }

    public string Description { get; private set; } =
        string.Empty;

    public string Owner { get; private set; } =
        string.Empty;

    public DateTimeOffset DueDateUtc { get; private set; }

    public MitigationStatus Status { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public static MitigationAction Create(
        Guid riskId,
        string description,
        string owner,
        DateTimeOffset dueDateUtc)
    {
        return new MitigationAction(
            id: Guid.NewGuid(),
            riskId: riskId,
            description: description.Trim(),
            owner: owner.Trim(),
            dueDateUtc: dueDateUtc,
            status: MitigationStatus.Planned,
            createdUtc: DateTimeOffset.UtcNow);
    }
}