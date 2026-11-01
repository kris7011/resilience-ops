namespace ResilienceOps.Api.Features.Incidents;

public sealed record IncidentHistoryResponse(
    Guid Id,
    Guid IncidentId,
    int Sequence,
    IncidentStatus? PreviousStatus,
    IncidentStatus NewStatus,
    DateTimeOffset OccurredUtc)
{
    public static IncidentHistoryResponse FromEntry(
        IncidentHistoryEntry entry)
    {
        return new IncidentHistoryResponse(
            Id: entry.Id,
            IncidentId: entry.IncidentId,
            Sequence: entry.Sequence,
            PreviousStatus: entry.PreviousStatus,
            NewStatus: entry.NewStatus,
            OccurredUtc: entry.OccurredUtc);
    }
}
