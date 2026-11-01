namespace ResilienceOps.Api.Features.Incidents;

public sealed class IncidentMitigationLink
{
    private IncidentMitigationLink()
    {
    }

    private IncidentMitigationLink(
        Guid incidentId,
        Guid mitigationId)
    {
        IncidentId = incidentId;
        MitigationId = mitigationId;
    }

    public Guid IncidentId { get; private set; }

    public Guid MitigationId { get; private set; }

    internal static IncidentMitigationLink Create(
        Guid incidentId,
        Guid mitigationId)
    {
        return new IncidentMitigationLink(
            incidentId,
            mitigationId);
    }
}
