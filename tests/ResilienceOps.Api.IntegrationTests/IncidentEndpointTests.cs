using System.Net;
using System.Net.Http.Json;

namespace ResilienceOps.Api.IntegrationTests;

public sealed class IncidentEndpointTests
    : IClassFixture<ResilienceOpsApiFactory>
{
    private readonly HttpClient client;

    public IncidentEndpointTests(
        ResilienceOpsApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateIncident_ValidRequest_ReturnsReportedIncidentWithHistory()
    {
        var riskId =
            await CreateRiskAsync();

        var mitigationId =
            await CreateMitigationAsync(riskId);

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/incidents",
                new
                {
                    title =
                        "  Identity provider unavailable  ",
                    description =
                        "Sign-in failed for all users for 20 minutes.",
                    responsiblePerson =
                        "Platform Operations",
                    mitigationIds =
                        new[] { mitigationId, mitigationId }
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var incident =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentApiResponse>();

        Assert.NotNull(incident);

        Assert.Equal(
            riskId,
            incident.RiskId);

        Assert.Equal(
            "Identity provider unavailable",
            incident.Title);

        Assert.Equal(
            "Platform Operations",
            incident.ResponsiblePerson);

        Assert.Equal(
            "Reported",
            incident.Status);

        Assert.Equal(
            [mitigationId],
            incident.MitigationIds);

        Assert.Null(incident.InvestigatingUtc);
        Assert.Null(incident.ResolvedUtc);
        Assert.Null(incident.ClosedUtc);

        var entry =
            Assert.Single(incident.History);

        Assert.Equal(1, entry.Sequence);
        Assert.Null(entry.PreviousStatus);
        Assert.Equal("Reported", entry.NewStatus);

        Assert.Equal(
            incident.ReportedUtc,
            entry.OccurredUtc);

        Assert.Equal(
            $"/api/risks/{riskId}/incidents/{incident.Id}",
            response.Headers.Location
                ?.OriginalString);
    }

    [Fact]
    public async Task CreateIncident_ValidRequest_PersistsIncident()
    {
        var riskId =
            await CreateRiskAsync();

        var created =
            await CreateIncidentAsync(riskId);

        var listResponse =
            await client.GetAsync(
                $"/api/risks/{riskId}/incidents");

        listResponse.EnsureSuccessStatusCode();

        var incidents =
            await listResponse.Content
                .ReadFromJsonAsync<
                    IncidentApiResponse[]>();

        Assert.NotNull(incidents);

        var listed =
            Assert.Single(incidents);

        Assert.Equal(created.Id, listed.Id);

        var getResponse =
            await client.GetAsync(
                $"/api/risks/{riskId}/incidents/{created.Id}");

        getResponse.EnsureSuccessStatusCode();

        var fetched =
            await getResponse.Content
                .ReadFromJsonAsync<
                    IncidentApiResponse>();

        Assert.NotNull(fetched);

        Assert.Equal(
            created.Title,
            fetched.Title);

        Assert.Equal(
            created.ReportedUtc,
            fetched.ReportedUtc);

        Assert.Single(fetched.History);
    }

    [Fact]
    public async Task IncidentEndpoints_UnknownRisk_ReturnNotFound()
    {
        var unknownRiskId = Guid.NewGuid();

        var listResponse =
            await client.GetAsync(
                $"/api/risks/{unknownRiskId}/incidents");

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/risks/{unknownRiskId}/incidents",
                ValidCreateRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            listResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            createResponse.StatusCode);
    }

    [Fact]
    public async Task CreateIncident_MissingFields_ReturnsValidationProblem()
    {
        var riskId =
            await CreateRiskAsync();

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/incidents",
                new
                {
                    title = " ",
                    description =
                        new string('x', 1001),
                    responsiblePerson =
                        (string?)null
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentValidationProblemResponse>();

        Assert.NotNull(problem);

        Assert.Contains(
            "Title is required.",
            problem.Errors["title"]);

        Assert.Contains(
            "Description cannot exceed 1000 characters.",
            problem.Errors["description"]);

        Assert.Contains(
            "Responsible person is required.",
            problem.Errors["responsiblePerson"]);
    }

    [Fact]
    public async Task CreateIncident_MitigationOfAnotherRisk_ReturnsValidationProblem()
    {
        var riskId =
            await CreateRiskAsync();

        var otherRiskId =
            await CreateRiskAsync();

        var foreignMitigationId =
            await CreateMitigationAsync(otherRiskId);

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/incidents",
                ValidCreateRequest(
                    foreignMitigationId));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentValidationProblemResponse>();

        Assert.NotNull(problem);

        Assert.True(
            problem.Errors.ContainsKey(
                "mitigationIds"));

        var listResponse =
            await client.GetFromJsonAsync<
                IncidentApiResponse[]>(
                $"/api/risks/{riskId}/incidents");

        Assert.NotNull(listResponse);
        Assert.Empty(listResponse);
    }

    [Fact]
    public async Task UpdateIncidentStatus_FullLifecycle_RecordsOrderedHistory()
    {
        var riskId =
            await CreateRiskAsync();

        var incident =
            await CreateIncidentAsync(riskId);

        var investigating =
            await ChangeStatusAsync(
                riskId,
                incident.Id,
                "Investigating");

        Assert.Equal(
            "Investigating",
            investigating.Status);

        Assert.NotNull(
            investigating.InvestigatingUtc);

        var resolved =
            await ChangeStatusAsync(
                riskId,
                incident.Id,
                "Resolved");

        Assert.NotNull(resolved.ResolvedUtc);

        var closed =
            await ChangeStatusAsync(
                riskId,
                incident.Id,
                "Closed");

        Assert.NotNull(closed.ClosedUtc);

        var reopened =
            await ChangeStatusAsync(
                riskId,
                incident.Id,
                "Investigating");

        Assert.Equal(
            "Investigating",
            reopened.Status);

        Assert.Null(reopened.ResolvedUtc);
        Assert.Null(reopened.ClosedUtc);

        var history =
            await client.GetFromJsonAsync<
                IncidentHistoryApiResponse[]>(
                $"/api/risks/{riskId}/incidents/{incident.Id}/history");

        Assert.NotNull(history);

        Assert.Equal(
            [1, 2, 3, 4, 5],
            history.Select(entry => entry.Sequence));

        Assert.Equal(
            [null, "Reported", "Investigating", "Resolved", "Closed"],
            history.Select(entry => entry.PreviousStatus));

        Assert.Equal(
            ["Reported", "Investigating", "Resolved", "Closed", "Investigating"],
            history.Select(entry => entry.NewStatus));

        Assert.Equal(
            history.Select(entry => entry.OccurredUtc).Order(),
            history.Select(entry => entry.OccurredUtc));
    }

    [Fact]
    public async Task UpdateIncidentStatus_ResolvedIncident_CanBeReopenedBeforeClosing()
    {
        var riskId =
            await CreateRiskAsync();

        var incident =
            await CreateIncidentAsync(riskId);

        await ChangeStatusAsync(
            riskId,
            incident.Id,
            "Investigating");

        await ChangeStatusAsync(
            riskId,
            incident.Id,
            "Resolved");

        var reopened =
            await ChangeStatusAsync(
                riskId,
                incident.Id,
                "Investigating");

        Assert.Equal(
            "Investigating",
            reopened.Status);

        Assert.Null(reopened.ResolvedUtc);

        Assert.Equal(
            4,
            reopened.History.Count);
    }

    [Fact]
    public async Task UpdateIncidentStatus_SkippedStep_ReturnsConflictAndKeepsStatus()
    {
        var riskId =
            await CreateRiskAsync();

        var incident =
            await CreateIncidentAsync(riskId);

        var response =
            await client.PatchAsJsonAsync(
                $"/api/risks/{riskId}/incidents/{incident.Id}/status",
                new
                {
                    status = "Closed"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "Cannot transition an incident from Reported to Closed.",
            error.Error);

        var fetched =
            await client.GetFromJsonAsync<
                IncidentApiResponse>(
                $"/api/risks/{riskId}/incidents/{incident.Id}");

        Assert.NotNull(fetched);

        Assert.Equal(
            "Reported",
            fetched.Status);

        Assert.Single(fetched.History);
    }

    [Fact]
    public async Task UpdateIncidentStatus_IncidentOfAnotherRisk_ReturnsNotFound()
    {
        var riskId =
            await CreateRiskAsync();

        var otherRiskId =
            await CreateRiskAsync();

        var incident =
            await CreateIncidentAsync(riskId);

        var updateResponse =
            await client.PatchAsJsonAsync(
                $"/api/risks/{otherRiskId}/incidents/{incident.Id}/status",
                new
                {
                    status = "Investigating"
                });

        var getResponse =
            await client.GetAsync(
                $"/api/risks/{otherRiskId}/incidents/{incident.Id}");

        var historyResponse =
            await client.GetAsync(
                $"/api/risks/{otherRiskId}/incidents/{incident.Id}/history");

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            historyResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateIncidentStatus_InvalidStatus_ReturnsValidationProblem()
    {
        var response =
            await client.PatchAsJsonAsync(
                $"/api/risks/{Guid.NewGuid()}/incidents/{Guid.NewGuid()}/status",
                new
                {
                    status = 999
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentValidationProblemResponse>();

        Assert.NotNull(problem);

        Assert.Contains(
            "Status is invalid.",
            problem.Errors["status"]);
    }

    private static object ValidCreateRequest(
        params Guid[] mitigationIds)
    {
        return new
        {
            title =
                "Payment provider timeouts",
            description =
                "Card payments timed out for 15 % of checkouts.",
            responsiblePerson =
                "Payments On-Call",
            mitigationIds
        };
    }

    private async Task<IncidentApiResponse> CreateIncidentAsync(
        Guid riskId)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/incidents",
                ValidCreateRequest());

        response.EnsureSuccessStatusCode();

        var incident =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentApiResponse>();

        Assert.NotNull(incident);

        return incident;
    }

    private async Task<IncidentApiResponse> ChangeStatusAsync(
        Guid riskId,
        Guid incidentId,
        string status)
    {
        var response =
            await client.PatchAsJsonAsync(
                $"/api/risks/{riskId}/incidents/{incidentId}/status",
                new
                {
                    status
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var incident =
            await response.Content
                .ReadFromJsonAsync<
                    IncidentApiResponse>();

        Assert.NotNull(incident);

        return incident;
    }

    private async Task<Guid> CreateRiskAsync()
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/risks",
                new
                {
                    title =
                        $"Incident test risk {Guid.NewGuid():N}",
                    description =
                        "A risk created to isolate incident tests.",
                    severity = "High"
                });

        response.EnsureSuccessStatusCode();

        var risk =
            await response.Content
                .ReadFromJsonAsync<
                    RiskLookupResponse>();

        Assert.NotNull(risk);

        return risk.Id;
    }

    private async Task<Guid> CreateMitigationAsync(
        Guid riskId)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/mitigations",
                new
                {
                    description =
                        "Fail over to the secondary provider.",
                    owner =
                        "Platform Operations",
                    dueDateUtc =
                        DateTimeOffset.UtcNow.AddDays(7)
                });

        response.EnsureSuccessStatusCode();

        var mitigation =
            await response.Content
                .ReadFromJsonAsync<
                    MitigationApiResponse>();

        Assert.NotNull(mitigation);

        return mitigation.Id;
    }
}

internal sealed record IncidentApiResponse(
    Guid Id,
    Guid RiskId,
    string Title,
    string Description,
    string ResponsiblePerson,
    string Status,
    Guid[] MitigationIds,
    DateTimeOffset ReportedUtc,
    DateTimeOffset? InvestigatingUtc,
    DateTimeOffset? ResolvedUtc,
    DateTimeOffset? ClosedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyList<IncidentHistoryApiResponse> History);

internal sealed record IncidentHistoryApiResponse(
    Guid Id,
    Guid IncidentId,
    int Sequence,
    string? PreviousStatus,
    string NewStatus,
    DateTimeOffset OccurredUtc);

internal sealed record IncidentErrorResponse(
    string Error);

internal sealed record IncidentValidationProblemResponse(
    Dictionary<string, string[]> Errors);
