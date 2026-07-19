namespace ResilienceOps.Api.Features.Risks;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/risks")
            .WithTags("Risks");

        group.MapGet("", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("", Create);

        return endpoints;
    }

    private static IResult GetAll(IRiskRepository repository)
    {
        var risks = repository
            .GetAll()
            .Select(RiskResponse.FromRisk)
            .ToArray();

        return Results.Ok(risks);
    }

    private static IResult GetById(
        Guid id,
        IRiskRepository repository)
    {
        var risk = repository.GetById(id);

        return risk is null
            ? Results.NotFound()
            : Results.Ok(RiskResponse.FromRisk(risk));
    }

    private static IResult Create(
        CreateRiskRequest request,
        IRiskRepository repository)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var risk = repository.Add(
            title: request.Title!,
            description: request.Description!,
            severity: request.Severity);

        var response = RiskResponse.FromRisk(risk);

        return Results.Created(
            $"/api/risks/{risk.Id}",
            response);
    }

    private static Dictionary<string, string[]> Validate(
        CreateRiskRequest request)
    {
        var errors = new Dictionary<string, string[]>();

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

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["description"] =
            [
                "Description is required."
            ];
        }
        else if (request.Description.Trim().Length > 1000)
        {
            errors["description"] =
            [
                "Description cannot exceed 1000 characters."
            ];
        }

        if (!Enum.IsDefined(typeof(RiskSeverity), request.Severity))
        {
            errors["severity"] =
            [
                "Severity is invalid."
            ];
        }

        return errors;
    }
}