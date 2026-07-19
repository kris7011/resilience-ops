using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Data;

namespace ResilienceOps.Api.Features.Mitigations;

public sealed class EfCoreMitigationRepository(
    ResilienceOpsDbContext dbContext,
    ILogger<EfCoreMitigationRepository> logger)
    : IMitigationRepository
{
    public async Task<IReadOnlyCollection<MitigationAction>>
        GetByRiskIdAsync(
            Guid riskId,
            CancellationToken cancellationToken)
    {
        using var activity =
            MitigationTelemetry.ActivitySource.StartActivity(
                "MitigationRepository.GetByRiskId",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            riskId.ToString());

        var mitigations =
            await dbContext.MitigationActions
                .AsNoTracking()
                .Where(
                    mitigation =>
                        mitigation.RiskId == riskId)
                .OrderBy(
                    mitigation =>
                        mitigation.DueDateUtc)
                .ThenBy(
                    mitigation =>
                        mitigation.CreatedUtc)
                .ToArrayAsync(cancellationToken);

        activity?.SetTag(
            "mitigation.count",
            mitigations.Length);

        logger.LogDebug(
            "Loaded {MitigationCount} mitigation actions for operational risk {RiskId}",
            mitigations.Length,
            riskId);

        return mitigations;
    }

    public async Task<MitigationAction?> GetByIdAsync(
        Guid riskId,
        Guid mitigationId,
        CancellationToken cancellationToken)
    {
        using var activity =
            MitigationTelemetry.ActivitySource.StartActivity(
                "MitigationRepository.GetById",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            riskId.ToString());

        activity?.SetTag(
            "mitigation.id",
            mitigationId.ToString());

        var mitigation =
            await dbContext.MitigationActions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == mitigationId
                        &&
                        item.RiskId == riskId,
                    cancellationToken);

        activity?.SetTag(
            "mitigation.found",
            mitigation is not null);

        return mitigation;
    }

    public async Task AddAsync(
        MitigationAction mitigation,
        CancellationToken cancellationToken)
    {
        using var activity =
            MitigationTelemetry.ActivitySource.StartActivity(
                "MitigationRepository.Add",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            mitigation.RiskId.ToString());

        activity?.SetTag(
            "mitigation.id",
            mitigation.Id.ToString());

        activity?.SetTag(
            "mitigation.status",
            mitigation.Status.ToString());

        dbContext.MitigationActions.Add(
            mitigation);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Persisted mitigation action {MitigationId} for operational risk {RiskId} with status {MitigationStatus}",
            mitigation.Id,
            mitigation.RiskId,
            mitigation.Status);
    }
}