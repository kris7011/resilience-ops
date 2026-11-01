namespace ResilienceOps.Api.Features.Incidents;

public sealed record IncidentResponse(
    Guid Id,
    Guid RiskId,
    string Title,
    string Description,
    string ResponsiblePerson,
    IncidentStatus Status,
    IReadOnlyCollection<Guid> MitigationIds,
    DateTimeOffset ReportedUtc,
    DateTimeOffset? InvestigatingUtc,
    DateTimeOffset? ResolvedUtc,
    DateTimeOffset? ClosedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyCollection<IncidentHistoryResponse> History)
{
    public static IncidentResponse FromIncident(
        Incident incident)
    {
        return new IncidentResponse(
            Id: incident.Id,
            RiskId: incident.RiskId,
            Title: incident.Title,
            Description: incident.Description,
            ResponsiblePerson:
                incident.ResponsiblePerson,
            Status: incident.Status,
            MitigationIds:
                incident.MitigationLinks
                    .Select(link => link.MitigationId)
                    .Order()
                    .ToArray(),
            ReportedUtc: incident.ReportedUtc,
            InvestigatingUtc:
                incident.InvestigatingUtc,
            ResolvedUtc: incident.ResolvedUtc,
            ClosedUtc: incident.ClosedUtc,
            UpdatedUtc: incident.UpdatedUtc,
            History:
                incident.History
                    .OrderBy(entry => entry.Sequence)
                    .Select(
                        IncidentHistoryResponse.FromEntry)
                    .ToArray());
    }
}
