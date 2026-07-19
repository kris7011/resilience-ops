using System.Net;
using System.Net.Http.Json;

namespace ResilienceOps.Api.IntegrationTests;

public sealed class MitigationEndpointTests
    : IClassFixture<ResilienceOpsApiFactory>
{
    private readonly HttpClient client;

    public MitigationEndpointTests(
        ResilienceOpsApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateMitigation_ValidRequest_ReturnsCreatedAction()
    {
        var riskId =
            await GetExistingRiskIdAsync();

        var request = new
        {
            description =
                "Document and test the identity provider fallback procedure.",
            owner =
                "Platform Operations",
            dueDateUtc =
                DateTimeOffset.UtcNow.AddDays(14)
        };

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/mitigations",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var mitigation =
            await response.Content
                .ReadFromJsonAsync<
                    MitigationApiResponse>();

        Assert.NotNull(mitigation);

        Assert.NotEqual(
            Guid.Empty,
            mitigation.Id);

        Assert.Equal(
            riskId,
            mitigation.RiskId);

        Assert.Equal(
            "Platform Operations",
            mitigation.Owner);

        Assert.Equal(
            "Planned",
            mitigation.Status);

        Assert.Equal(
            $"/api/risks/{riskId}/mitigations/{mitigation.Id}",
            response.Headers.Location
                ?.OriginalString);
    }

    [Fact]
    public async Task CreateMitigation_ValidRequest_PersistsAction()
    {
        var riskId =
            await GetExistingRiskIdAsync();

        var description =
            $"Run recovery exercise {Guid.NewGuid():N}";

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/mitigations",
                new
                {
                    description,
                    owner =
                        "Business Continuity",
                    dueDateUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(21)
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdMitigation =
            await createResponse.Content
                .ReadFromJsonAsync<
                    MitigationApiResponse>();

        Assert.NotNull(createdMitigation);

        var getResponse =
            await client.GetAsync(
                $"/api/risks/{riskId}/mitigations/{createdMitigation.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var persistedMitigation =
            await getResponse.Content
                .ReadFromJsonAsync<
                    MitigationApiResponse>();

        Assert.NotNull(persistedMitigation);

        Assert.Equal(
            createdMitigation.Id,
            persistedMitigation.Id);

        Assert.Equal(
            description,
            persistedMitigation.Description);

        Assert.Equal(
            "Planned",
            persistedMitigation.Status);
    }

    [Fact]
    public async Task GetMitigations_ReturnsCreatedAction()
    {
        var riskId =
            await GetExistingRiskIdAsync();

        var description =
            $"Verify alternate supplier {Guid.NewGuid():N}";

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/mitigations",
                new
                {
                    description,
                    owner =
                        "Supplier Management",
                    dueDateUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(30)
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var getResponse =
            await client.GetAsync(
                $"/api/risks/{riskId}/mitigations");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var mitigations =
            await getResponse.Content
                .ReadFromJsonAsync<
                    MitigationApiResponse[]>();

        Assert.NotNull(mitigations);

        Assert.Contains(
            mitigations,
            mitigation =>
                mitigation.Description ==
                description);
    }

    [Fact]
    public async Task CreateMitigation_UnknownRisk_ReturnsNotFound()
    {
        var unknownRiskId =
            Guid.NewGuid();

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{unknownRiskId}/mitigations",
                new
                {
                    description =
                        "Test fallback operation.",
                    owner =
                        "Platform Operations",
                    dueDateUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(10)
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateMitigation_MissingOwner_ReturnsValidationProblem()
    {
        var riskId =
            await GetExistingRiskIdAsync();

        var response =
            await client.PostAsJsonAsync(
                $"/api/risks/{riskId}/mitigations",
                new
                {
                    description =
                        "Document the escalation procedure.",
                    owner = " ",
                    dueDateUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(10)
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var validationProblem =
            await response.Content
                .ReadFromJsonAsync<
                    MitigationValidationProblemResponse>();

        Assert.NotNull(validationProblem);

        Assert.True(
            validationProblem.Errors.TryGetValue(
                "owner",
                out var ownerErrors));

        Assert.Contains(
            "Owner is required.",
            ownerErrors);
    }

    private async Task<Guid> GetExistingRiskIdAsync()
    {
        var response =
            await client.GetAsync(
                "/api/risks");

        response.EnsureSuccessStatusCode();

        var risks =
            await response.Content
                .ReadFromJsonAsync<
                    RiskLookupResponse[]>();

        Assert.NotNull(risks);
        Assert.NotEmpty(risks);

        return risks[0].Id;
    }
}

internal sealed record RiskLookupResponse(
    Guid Id);

internal sealed record MitigationApiResponse(
    Guid Id,
    Guid RiskId,
    string Description,
    string Owner,
    DateTimeOffset DueDateUtc,
    string Status,
    DateTimeOffset CreatedUtc);

internal sealed record MitigationValidationProblemResponse(
    Dictionary<string, string[]> Errors);