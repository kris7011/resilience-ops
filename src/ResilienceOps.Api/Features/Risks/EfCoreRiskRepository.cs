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
}