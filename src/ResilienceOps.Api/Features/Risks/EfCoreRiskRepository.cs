using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Data;

namespace ResilienceOps.Api.Features.Risks;

public sealed class EfCoreRiskRepository(
    ResilienceOpsDbContext dbContext,
    ILogger<EfCoreRiskRepository> logger)
    : IRiskRepository
{
    public async Task<IReadOnlyCollection<RiskItem>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        using var activity =
            RiskTelemetry.ActivitySource.StartActivity(
                "RiskRepository.GetAll",
                ActivityKind.Internal);

        var risks = await dbContext.Risks
            .AsNoTracking()
            .OrderByDescending(risk => risk.CreatedUtc)
            .ToArrayAsync(cancellationToken);

        activity?.SetTag(
            "risk.count",
            risks.Length);

        logger.LogDebug(
            "Loaded {RiskCount} operational risks",
            risks.Length);

        return risks;
    }

    public async Task<RiskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            RiskTelemetry.ActivitySource.StartActivity(
                "RiskRepository.GetById",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            id.ToString());

        var risk = await dbContext.Risks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                risk => risk.Id == id,
                cancellationToken);

        activity?.SetTag(
            "risk.found",
            risk is not null);

        logger.LogDebug(
            "Operational risk {RiskId} was {Result}",
            id,
            risk is null ? "not found" : "found");

        return risk;
    }

    public async Task AddAsync(
        RiskItem risk,
        CancellationToken cancellationToken)
    {
        using var activity =
            RiskTelemetry.ActivitySource.StartActivity(
                "RiskRepository.Add",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            risk.Id.ToString());

        activity?.SetTag(
            "risk.severity",
            risk.Severity.ToString());

        activity?.SetTag(
            "risk.status",
            risk.Status.ToString());

        dbContext.Risks.Add(risk);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Persisted operational risk {RiskId} with severity {RiskSeverity} and status {RiskStatus}",
            risk.Id,
            risk.Severity,
            risk.Status);
    }

    public async Task<RiskItem?> UpdateStatusAsync(
        Guid id,
        RiskStatus status,
        CancellationToken cancellationToken)
    {
        using var activity =
            RiskTelemetry.ActivitySource.StartActivity(
                "RiskRepository.UpdateStatus",
                ActivityKind.Internal);

        activity?.SetTag(
            "risk.id",
            id.ToString());

        activity?.SetTag(
            "risk.requested_status",
            status.ToString());

        var risk = await dbContext.Risks
            .SingleOrDefaultAsync(
                risk => risk.Id == id,
                cancellationToken);

        if (risk is null)
        {
            activity?.SetTag(
                "risk.found",
                false);

            logger.LogDebug(
                "Operational risk {RiskId} was not found during status update",
                id);

            return null;
        }

        activity?.SetTag(
            "risk.found",
            true);

        var previousStatus = risk.Status;

        var changed =
            risk.ChangeStatus(status);

        activity?.SetTag(
            "risk.previous_status",
            previousStatus.ToString());

        activity?.SetTag(
            "risk.new_status",
            risk.Status.ToString());

        activity?.SetTag(
            "risk.status_changed",
            changed);

        if (changed)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            logger.LogInformation(
                "Changed operational risk {RiskId} status from {PreviousStatus} to {NewStatus}",
                risk.Id,
                previousStatus,
                risk.Status);
        }
        else
        {
            logger.LogDebug(
                "Operational risk {RiskId} already had status {RiskStatus}",
                risk.Id,
                risk.Status);
        }

        return risk;
    }
}