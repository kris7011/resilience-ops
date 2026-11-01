namespace ResilienceOps.Api.Features.Incidents;

public sealed class Incident
{
    private readonly List<IncidentMitigationLink>
        mitigationLinks = [];

    private readonly List<IncidentHistoryEntry>
        history = [];

    private Incident()
    {
    }

    private Incident(
        Guid id,
        Guid riskId,
        string title,
        string description,
        string responsiblePerson,
        DateTimeOffset reportedUtc)
    {
        Id = id;
        RiskId = riskId;
        Title = title;
        Description = description;
        ResponsiblePerson = responsiblePerson;
        Status = IncidentStatus.Reported;
        ReportedUtc = reportedUtc;
        UpdatedUtc = reportedUtc;
    }

    public Guid Id { get; private set; }

    public Guid RiskId { get; private set; }

    public string Title { get; private set; } =
        string.Empty;

    public string Description { get; private set; } =
        string.Empty;

    public string ResponsiblePerson { get; private set; } =
        string.Empty;

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset ReportedUtc { get; private set; }

    public DateTimeOffset? InvestigatingUtc { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public IReadOnlyCollection<IncidentMitigationLink>
        MitigationLinks => mitigationLinks;

    public IReadOnlyCollection<IncidentHistoryEntry>
        History => history;

    public static Incident Create(
        Guid riskId,
        string title,
        string description,
        string responsiblePerson,
        IEnumerable<Guid> mitigationIds)
    {
        if (riskId == Guid.Empty)
        {
            throw new ArgumentException(
                "Risk id is required.",
                nameof(riskId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Title is required.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Description is required.",
                nameof(description));
        }

        if (string.IsNullOrWhiteSpace(responsiblePerson))
        {
            throw new ArgumentException(
                "Responsible person is required.",
                nameof(responsiblePerson));
        }

        var now = UtcNowToMilliseconds();

        var incident = new Incident(
            id: Guid.NewGuid(),
            riskId: riskId,
            title: title.Trim(),
            description: description.Trim(),
            responsiblePerson:
                responsiblePerson.Trim(),
            reportedUtc: now);

        foreach (var mitigationId in
            mitigationIds.Distinct())
        {
            incident.mitigationLinks.Add(
                IncidentMitigationLink.Create(
                    incident.Id,
                    mitigationId));
        }

        incident.history.Add(
            IncidentHistoryEntry.Create(
                incidentId: incident.Id,
                sequence: 1,
                previousStatus: null,
                newStatus: IncidentStatus.Reported,
                occurredUtc: now));

        return incident;
    }

    public void ChangeStatus(
        IncidentStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(newStatus),
                newStatus,
                "The incident status is invalid.");
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition an incident from {Status} to {newStatus}.");
        }

        var previousStatus = Status;
        var occurredUtc = UtcNowToMilliseconds();

        Status = newStatus;
        UpdatedUtc = occurredUtc;

        switch (newStatus)
        {
            case IncidentStatus.Investigating:
                InvestigatingUtc = occurredUtc;

                // Reopening clears the resolution; the history keeps it.
                ResolvedUtc = null;
                ClosedUtc = null;

                break;

            case IncidentStatus.Resolved:
                ResolvedUtc = occurredUtc;
                break;

            case IncidentStatus.Closed:
                ClosedUtc = occurredUtc;
                break;

            case IncidentStatus.Reported:
            default:
                throw new InvalidOperationException(
                    "The requested incident transition is not supported.");
        }

        history.Add(
            IncidentHistoryEntry.Create(
                incidentId: Id,
                sequence: history.Count + 1,
                previousStatus: previousStatus,
                newStatus: newStatus,
                occurredUtc: occurredUtc));
    }

    // Timestamps are stored as Unix milliseconds, so they are truncated here
    // to keep API responses identical to what is read back later.
    private static DateTimeOffset UtcNowToMilliseconds()
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    // Lifecycle: Reported -> Investigating -> Resolved -> Closed. A resolved
    // or closed incident can be reopened by moving it back to Investigating.
    private bool CanTransitionTo(
        IncidentStatus newStatus)
    {
        return (Status, newStatus) switch
        {
            (
                IncidentStatus.Reported,
                IncidentStatus.Investigating
            ) => true,
            (
                IncidentStatus.Investigating,
                IncidentStatus.Resolved
            ) => true,
            (
                IncidentStatus.Resolved,
                IncidentStatus.Closed
            ) => true,
            (
                IncidentStatus.Resolved,
                IncidentStatus.Investigating
            ) => true,
            (
                IncidentStatus.Closed,
                IncidentStatus.Investigating
            ) => true,
            _ => false
        };
    }
}
