using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Data;

namespace ResilienceOps.Api.Features.Incidents;

public sealed class EfCoreIncidentRepository(
    ResilienceOpsDbContext dbContext,
    ILogger<EfCoreIncidentRepository> logger)
    : IIncidentRepository
{
    public async Task<IReadOnlyCollection<Incident>>
        GetByRiskIdAsync(
            Guid riskId,
            CancellationToken cancellationToken)
    {
        using var activity =
            IncidentTelemetry.ActivitySource.StartActivity(
                "IncidentRepository.GetByRiskId",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            riskId.ToString());

        var incidents =
            await IncidentQuery(tracking: false)
                .Where(
                    incident =>
                        incident.RiskId == riskId)
                .OrderByDescending(
                    incident =>
                        incident.ReportedUtc)
                .ToArrayAsync(cancellationToken);

        activity?.SetTag(
            "incident.count",
            incidents.Length);

        logger.LogDebug(
            "Loaded {IncidentCount} incidents for operational risk {RiskId}",
            incidents.Length,
            riskId);

        return incidents;
    }

    public async Task<Incident?> GetByIdAsync(
        Guid riskId,
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        using var activity =
            IncidentTelemetry.ActivitySource.StartActivity(
                "IncidentRepository.GetById",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            riskId.ToString());

        activity?.SetTag(
            "incident.id",
            incidentId.ToString());

        return await IncidentQuery(tracking: false)
            .SingleOrDefaultAsync(
                incident =>
                    incident.Id == incidentId
                    &&
                    incident.RiskId == riskId,
                cancellationToken);
    }

    public async Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken)
    {
        using var activity =
            IncidentTelemetry.ActivitySource.StartActivity(
                "IncidentRepository.Add",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            incident.RiskId.ToString());

        activity?.SetTag(
            "incident.id",
            incident.Id.ToString());

        activity?.SetTag(
            "incident.status",
            incident.Status.ToString());

        dbContext.Incidents.Add(incident);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Persisted incident {IncidentId} for operational risk {RiskId} with status {IncidentStatus}",
            incident.Id,
            incident.RiskId,
            incident.Status);
    }

    public async Task<IncidentStatusUpdateResult>
        UpdateStatusAsync(
            Guid riskId,
            Guid incidentId,
            IncidentStatus status,
            CancellationToken cancellationToken)
    {
        using var activity =
            IncidentTelemetry.ActivitySource.StartActivity(
                "IncidentRepository.UpdateStatus",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            riskId.ToString());

        activity?.SetTag(
            "incident.id",
            incidentId.ToString());

        activity?.SetTag(
            "incident.requested_status",
            status.ToString());

        var incident =
            await IncidentQuery(tracking: true)
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == incidentId
                        &&
                        item.RiskId == riskId,
                    cancellationToken);

        if (incident is null)
        {
            return new IncidentStatusUpdateResult(
                IncidentStatusUpdateOutcome.NotFound,
                Incident: null,
                Error: null);
        }

        var previousStatus = incident.Status;

        try
        {
            incident.ChangeStatus(status);
        }
        catch (InvalidOperationException exception)
        {
            activity?.SetTag(
                "incident.transition_valid",
                false);

            return new IncidentStatusUpdateResult(
                IncidentStatusUpdateOutcome.InvalidTransition,
                incident,
                exception.Message);
        }

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Two requests that start from the same status both append the
            // next history sequence; the unique (IncidentId, Sequence) index
            // lets only the first one commit.
            logger.LogWarning(
                exception,
                "Concurrent status change rejected for incident {IncidentId}",
                incident.Id);

            return new IncidentStatusUpdateResult(
                IncidentStatusUpdateOutcome.Conflict,
                Incident: null,
                Error: "The incident was changed by another request. Reload it and try again.");
        }

        activity?.SetTag(
            "incident.transition_valid",
            true);

        logger.LogInformation(
            "Changed incident {IncidentId} status from {PreviousStatus} to {NewStatus}",
            incident.Id,
            previousStatus,
            incident.Status);

        return new IncidentStatusUpdateResult(
            IncidentStatusUpdateOutcome.Updated,
            incident,
            Error: null);
    }

    private IQueryable<Incident> IncidentQuery(
        bool tracking)
    {
        var query = dbContext.Incidents
            .Include(
                incident =>
                    incident.MitigationLinks)
            .Include(
                incident =>
                    incident.History)
            .AsSplitQuery();

        return tracking
            ? query
            : query.AsNoTracking();
    }
}
