namespace ResilienceOps.Api.Features.Risks;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/risks")
            .WithTags("Risks");

        group.MapGet("", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("", CreateAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        IRiskRepository repository,
        CancellationToken cancellationToken)
    {
        var riskItems =
            await repository.GetAllAsync(
                cancellationToken);

        var responses = riskItems
            .Select(RiskResponse.FromRisk)
            .ToArray();

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IRiskRepository repository,
        CancellationToken cancellationToken)
    {
        var risk =
            await repository.GetByIdAsync(
                id,
                cancellationToken);

        return risk is null
            ? Results.NotFound()
            : Results.Ok(
                RiskResponse.FromRisk(risk));
    }

    private static async Task<IResult> CreateAsync(
        CreateRiskRequest request,
        IRiskRepository repository,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var risk = RiskItem.Create(
            title: request.Title!,
            description: request.Description!,
            severity: request.Severity);

        await repository.AddAsync(
            risk,
            cancellationToken);

        var response =
            RiskResponse.FromRisk(risk);

        return Results.Created(
            $"/api/risks/{risk.Id}",
            response);
    }

    private static Dictionary<string, string[]> Validate(
        CreateRiskRequest request)
    {
        var errors =
            new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] =
            [
                "Title is required."
            ];
        }
        else if (request.Title.Trim().Length > 120)
        {
            errors["title"] =
            [
                "Title cannot exceed 120 characters."
            ];
        }

        if (string.IsNullOrWhiteSpace(
                request.Description))
        {
            errors["description"] =
            [
                "Description is required."
            ];
        }
        else if (
            request.Description.Trim().Length > 1000)
        {
            errors["description"] =
            [
                "Description cannot exceed 1000 characters."
            ];
        }

        if (!Enum.IsDefined(
                typeof(RiskSeverity),
                request.Severity))
        {
            errors["severity"] =
            [
                "Severity is invalid."
            ];
        }

        return errors;
    }
}