namespace ResilienceOps.Api.Features.Incidents;

public sealed class IncidentHistoryEntry
{
    private IncidentHistoryEntry()
    {
    }

    private IncidentHistoryEntry(
        Guid id,
        Guid incidentId,
        int sequence,
        IncidentStatus? previousStatus,
        IncidentStatus newStatus,
        DateTimeOffset occurredUtc)
    {
        Id = id;
        IncidentId = incidentId;
        Sequence = sequence;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        OccurredUtc = occurredUtc;
    }

    public Guid Id { get; private set; }

    public Guid IncidentId { get; private set; }

    public int Sequence { get; private set; }

    public IncidentStatus? PreviousStatus { get; private set; }

    public IncidentStatus NewStatus { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    internal static IncidentHistoryEntry Create(
        Guid incidentId,
        int sequence,
        IncidentStatus? previousStatus,
        IncidentStatus newStatus,
        DateTimeOffset occurredUtc)
    {
        return new IncidentHistoryEntry(
            id: Guid.NewGuid(),
            incidentId: incidentId,
            sequence: sequence,
            previousStatus: previousStatus,
            newStatus: newStatus,
            occurredUtc: occurredUtc);
    }
}
