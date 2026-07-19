using System.Collections.Concurrent;

namespace ResilienceOps.Api.Features.Risks;

public sealed class InMemoryRiskRepository : IRiskRepository
{
    private readonly ConcurrentDictionary<Guid, RiskItem> risks = new();

    public InMemoryRiskRepository()
    {
        Add(
            title: "Identity provider outage",
            description:
                "Loss of the identity provider could prevent employees from accessing critical systems.",
            severity: RiskSeverity.Critical);

        Add(
            title: "Delayed incident escalation",
            description:
                "Unclear ownership may delay escalation of high-impact operational incidents.",
            severity: RiskSeverity.High);
    }

    public IReadOnlyCollection<RiskItem> GetAll()
    {
        return risks.Values
            .OrderByDescending(risk => risk.CreatedUtc)
            .ToArray();
    }

    public RiskItem? GetById(Guid id)
    {
        return risks.GetValueOrDefault(id);
    }

    public RiskItem Add(
        string title,
        string description,
        RiskSeverity severity)
    {
        var risk = RiskItem.Create(
            title,
            description,
            severity);

        if (!risks.TryAdd(risk.Id, risk))
        {
            throw new InvalidOperationException(
                $"A risk with ID '{risk.Id}' already exists.");
        }

        return risk;
    }
}