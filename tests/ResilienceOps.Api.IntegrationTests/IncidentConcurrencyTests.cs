using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ResilienceOps.Api.Data;
using ResilienceOps.Api.Features.Incidents;

namespace ResilienceOps.Api.IntegrationTests;

public sealed class IncidentConcurrencyTests
    : IClassFixture<ResilienceOpsApiFactory>
{
    private readonly ResilienceOpsApiFactory factory;

    public IncidentConcurrencyTests(
        ResilienceOpsApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangedByAnotherRequestBeforeSave_ReturnsConflict()
    {
        var client = factory.CreateClient();

        var riskResponse =
            await client.PostAsJsonAsync(
                "/api/risks",
                new
                {
                    title =
                        $"Concurrency risk {Guid.NewGuid():N}",
                    description =
                        "A risk used to race two status changes.",
                    severity = "Medium"
                });

        riskResponse.EnsureSuccessStatusCode();

        var risk =
            await riskResponse.Content
                .ReadFromJsonAsync<RiskLookupResponse>();

        Assert.NotNull(risk);

        var incidentResponse =
            await client.PostAsJsonAsync(
                $"/api/risks/{risk.Id}/incidents",
                new
                {
                    title = "Queue backlog",
                    description =
                        "Messages waited more than ten minutes.",
                    responsiblePerson = "Integration Team"
                });

        incidentResponse.EnsureSuccessStatusCode();

        var created =
            await incidentResponse.Content
                .ReadFromJsonAsync<IncidentApiResponse>();

        Assert.NotNull(created);

        using var scope =
            factory.Services.CreateScope();

        var connection =
            scope.ServiceProvider
                .GetRequiredService<ResilienceOpsDbContext>()
                .Database
                .GetDbConnection();

        // The competing request commits after this request has loaded the
        // incident but before it saves, which is the race being guarded.
        var competingChange =
            new CompetingChangeInterceptor(
                () => MoveToInvestigating(
                    connection,
                    created.Id));

        await using var dbContext =
            new ResilienceOpsDbContext(
                new DbContextOptionsBuilder<ResilienceOpsDbContext>()
                    .UseSqlite(connection)
                    .AddInterceptors(competingChange)
                    .Options);

        var repository =
            new EfCoreIncidentRepository(
                dbContext,
                NullLogger<EfCoreIncidentRepository>.Instance);

        var result =
            await repository.UpdateStatusAsync(
                risk.Id,
                created.Id,
                IncidentStatus.Investigating,
                CancellationToken.None);

        Assert.Equal(
            IncidentStatusUpdateOutcome.Conflict,
            result.Outcome);

        var stored =
            await client.GetFromJsonAsync<IncidentApiResponse>(
                $"/api/risks/{risk.Id}/incidents/{created.Id}");

        Assert.NotNull(stored);

        Assert.Equal(
            "Investigating",
            stored.Status);

        Assert.Equal(
            2,
            stored.History.Count);
    }

    private static void MoveToInvestigating(
        DbConnection connection,
        Guid incidentId)
    {
        using var competitor =
            new ResilienceOpsDbContext(
                new DbContextOptionsBuilder<ResilienceOpsDbContext>()
                    .UseSqlite(connection)
                    .Options);

        var incident =
            competitor.Incidents
                .Include(item => item.History)
                .Single(item => item.Id == incidentId);

        incident.ChangeStatus(
            IncidentStatus.Investigating);

        competitor.SaveChanges();
    }

    private sealed class CompetingChangeInterceptor(
        Action competingChange)
        : SaveChangesInterceptor
    {
        private bool hasRun;

        public override ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            if (!hasRun)
            {
                hasRun = true;
                competingChange();
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }
}
