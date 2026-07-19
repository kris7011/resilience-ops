using System.Net;
using System.Net.Http.Json;

namespace ResilienceOps.Api.IntegrationTests;

public sealed class RiskEndpointTests
    : IClassFixture<ResilienceOpsApiFactory>
{
    private readonly HttpClient client;

    public RiskEndpointTests(
        ResilienceOpsApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRisks_ReturnsSeededRisks()
    {
        var response = await client.GetAsync(
            "/api/risks");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var risks =
            await response.Content
                .ReadFromJsonAsync<
                    RiskApiResponse[]>();

        Assert.NotNull(risks);

        Assert.Contains(
            risks,
            risk =>
                risk.Title ==
                "Identity provider outage"
                &&
                risk.Severity == "Critical"
                &&
                risk.Status == "Open");

        Assert.Contains(
            risks,
            risk =>
                risk.Title ==
                "Delayed incident escalation"
                &&
                risk.Severity == "High"
                &&
                risk.Status == "Open");
    }

    [Fact]
    public async Task CreateRisk_ValidRequest_ReturnsCreatedRisk()
    {
        var title =
            $"Cloud region outage {Guid.NewGuid():N}";

        var request = new
        {
            title,
            description =
                "A regional outage could make the customer platform unavailable.",
            severity = "High"
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/risks",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var createdRisk =
            await response.Content
                .ReadFromJsonAsync<
                    RiskApiResponse>();

        Assert.NotNull(createdRisk);

        Assert.NotEqual(
            Guid.Empty,
            createdRisk.Id);

        Assert.Equal(
            title,
            createdRisk.Title);

        Assert.Equal(
            "High",
            createdRisk.Severity);

        Assert.Equal(
            "Open",
            createdRisk.Status);

        Assert.Equal(
            $"/api/risks/{createdRisk.Id}",
            response.Headers.Location
                ?.OriginalString);
    }

    [Fact]
    public async Task CreateRisk_ValidRequest_PersistsRisk()
    {
        var title =
            $"Payment dependency {Guid.NewGuid():N}";

        var createRequest = new
        {
            title,
            description =
                "Loss of the payment provider could prevent customer transactions.",
            severity = "Critical"
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/risks",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdRisk =
            await createResponse.Content
                .ReadFromJsonAsync<
                    RiskApiResponse>();

        Assert.NotNull(createdRisk);

        var getResponse =
            await client.GetAsync(
                $"/api/risks/{createdRisk.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var persistedRisk =
            await getResponse.Content
                .ReadFromJsonAsync<
                    RiskApiResponse>();

        Assert.NotNull(persistedRisk);

        Assert.Equal(
            createdRisk.Id,
            persistedRisk.Id);

        Assert.Equal(
            title,
            persistedRisk.Title);

        Assert.Equal(
            "Critical",
            persistedRisk.Severity);
    }

    [Fact]
    public async Task CreateRisk_MissingTitle_ReturnsValidationProblem()
    {
        var request = new
        {
            title = " ",
            description =
                "The description is valid, but the title is missing.",
            severity = "Medium"
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/risks",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var validationProblem =
            await response.Content
                .ReadFromJsonAsync<
                    ValidationProblemResponse>();

        Assert.NotNull(validationProblem);

        Assert.True(
            validationProblem.Errors.TryGetValue(
                "title",
                out var titleErrors));

        Assert.Contains(
            "Title is required.",
            titleErrors);
    }

    [Fact]
    public async Task GetRisk_UnknownId_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid();

        var response =
            await client.GetAsync(
                $"/api/risks/{unknownId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}

internal sealed record RiskApiResponse(
    Guid Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    DateTimeOffset CreatedUtc);

internal sealed record ValidationProblemResponse(
    Dictionary<string, string[]> Errors);