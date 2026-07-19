using Microsoft.EntityFrameworkCore;
using ResilienceOps.Api.Data;

namespace ResilienceOps.Api.Features.Risks;

public sealed class EfCoreRiskRepository(
    ResilienceOpsDbContext dbContext)
    : IRiskRepository
{
    public async Task<IReadOnlyCollection<RiskItem>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Risks
            .AsNoTracking()
            .OrderByDescending(risk => risk.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<RiskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Risks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                risk => risk.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        RiskItem risk,
        CancellationToken cancellationToken)
    {
        dbContext.Risks.Add(risk);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}