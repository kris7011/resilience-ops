using ResilienceOps.Api.Features.Risks;

namespace ResilienceOps.Api.Features.Mitigations;

public static class MitigationEndpoints
{
    public static IEndpointRouteBuilder MapMitigationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/risks/{riskId:guid}/mitigations")
            .WithTags("Mitigations");

        group.MapGet(
            "",
            GetAllAsync);

        group.MapGet(
            "/{mitigationId:guid}",
            GetByIdAsync);

        group.MapPost(
            "",
            CreateAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        Guid riskId,
        IRiskRepository riskRepository,
        IMitigationRepository mitigationRepository,
        CancellationToken cancellationToken)
    {
        var risk =
            await riskRepository.GetByIdAsync(
                riskId,
                cancellationToken);

        if (risk is null)
        {
            return Results.NotFound();
        }

        var mitigations =
            await mitigationRepository.GetByRiskIdAsync(
                riskId,
                cancellationToken);

        var responses = mitigations
            .Select(
                MitigationResponse.FromMitigation)
            .ToArray();

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid riskId,
        Guid mitigationId,
        IMitigationRepository repository,
        CancellationToken cancellationToken)
    {
        var mitigation =
            await repository.GetByIdAsync(
                riskId,
                mitigationId,
                cancellationToken);

        return mitigation is null
            ? Results.NotFound()
            : Results.Ok(
                MitigationResponse.FromMitigation(
                    mitigation));
    }

    private static async Task<IResult> CreateAsync(
        Guid riskId,
        CreateMitigationRequest request,
        IRiskRepository riskRepository,
        IMitigationRepository mitigationRepository,
        CancellationToken cancellationToken)
    {
        var validationErrors =
            Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var risk =
            await riskRepository.GetByIdAsync(
                riskId,
                cancellationToken);

        if (risk is null)
        {
            return Results.NotFound();
        }

        var mitigation =
            MitigationAction.Create(
                riskId: riskId,
                description: request.Description!,
                owner: request.Owner!,
                dueDateUtc: request.DueDateUtc);

        await mitigationRepository.AddAsync(
            mitigation,
            cancellationToken);

        var response =
            MitigationResponse.FromMitigation(
                mitigation);

        return Results.Created(
            $"/api/risks/{riskId}/mitigations/{mitigation.Id}",
            response);
    }

    private static Dictionary<string, string[]> Validate(
        CreateMitigationRequest request)
    {
        var errors =
            new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(
                request.Description))
        {
            errors["description"] =
            [
                "Description is required."
            ];
        }
        else if (
            request.Description.Trim().Length > 500)
        {
            errors["description"] =
            [
                "Description cannot exceed 500 characters."
            ];
        }

        if (string.IsNullOrWhiteSpace(
                request.Owner))
        {
            errors["owner"] =
            [
                "Owner is required."
            ];
        }
        else if (
            request.Owner.Trim().Length > 120)
        {
            errors["owner"] =
            [
                "Owner cannot exceed 120 characters."
            ];
        }

        if (request.DueDateUtc == default)
        {
            errors["dueDateUtc"] =
            [
                "Due date is required."
            ];
        }
        else if (
            request.DueDateUtc <=
            DateTimeOffset.UtcNow)
        {
            errors["dueDateUtc"] =
            [
                "Due date must be in the future."
            ];
        }

        return errors;
    }
}