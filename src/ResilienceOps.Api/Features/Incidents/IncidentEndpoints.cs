using ResilienceOps.Api.Features.Mitigations;
using ResilienceOps.Api.Features.Risks;

namespace ResilienceOps.Api.Features.Incidents;

public static class IncidentEndpoints
{
    private const int MaximumTitleLength = 120;
    private const int MaximumDescriptionLength = 1000;
    private const int MaximumResponsiblePersonLength = 120;
    private const int MaximumMitigationLinks = 50;

    public static IEndpointRouteBuilder MapIncidentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/risks/{riskId:guid}/incidents")
            .WithTags("Incidents");

        group.MapGet("", GetAllAsync);
        group.MapGet(
            "/{incidentId:guid}",
            GetByIdAsync);
        group.MapGet(
            "/{incidentId:guid}/history",
            GetHistoryAsync);
        group.MapPost("", CreateAsync);
        group.MapPatch(
            "/{incidentId:guid}/status",
            UpdateStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        Guid riskId,
        IRiskRepository riskRepository,
        IIncidentRepository incidentRepository,
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

        var incidents =
            await incidentRepository.GetByRiskIdAsync(
                riskId,
                cancellationToken);

        return Results.Ok(
            incidents
                .Select(IncidentResponse.FromIncident)
                .ToArray());
    }

    private static async Task<IResult> GetByIdAsync(
        Guid riskId,
        Guid incidentId,
        IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var incident =
            await repository.GetByIdAsync(
                riskId,
                incidentId,
                cancellationToken);

        return incident is null
            ? Results.NotFound()
            : Results.Ok(
                IncidentResponse.FromIncident(incident));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid riskId,
        Guid incidentId,
        IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var incident =
            await repository.GetByIdAsync(
                riskId,
                incidentId,
                cancellationToken);

        if (incident is null)
        {
            return Results.NotFound();
        }

        var history = incident.History
            .OrderBy(entry => entry.Sequence)
            .Select(
                IncidentHistoryResponse.FromEntry)
            .ToArray();

        return Results.Ok(history);
    }

    private static async Task<IResult> CreateAsync(
        Guid riskId,
        CreateIncidentRequest request,
        IRiskRepository riskRepository,
        IMitigationRepository mitigationRepository,
        IIncidentRepository incidentRepository,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

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

        var mitigationIds =
            request.MitigationIds?
                .Distinct()
                .ToArray()
            ?? [];

        if (mitigationIds.Length > 0)
        {
            var riskMitigations =
                await mitigationRepository
                    .GetByRiskIdAsync(
                        riskId,
                        cancellationToken);

            var riskMitigationIds =
                riskMitigations
                    .Select(
                        mitigation =>
                            mitigation.Id)
                    .ToHashSet();

            if (
                mitigationIds.Any(
                    mitigationId =>
                        !riskMitigationIds.Contains(
                            mitigationId)))
            {
                return Results.ValidationProblem(
                    new Dictionary<
                        string,
                        string[]>
                    {
                        ["mitigationIds"] =
                        [
                            "Every linked mitigation action must belong to the incident's risk."
                        ]
                    });
            }
        }

        var incident = Incident.Create(
            riskId: riskId,
            title: request.Title!,
            description: request.Description!,
            responsiblePerson:
                request.ResponsiblePerson!,
            mitigationIds: mitigationIds);

        await incidentRepository.AddAsync(
            incident,
            cancellationToken);

        return Results.Created(
            $"/api/risks/{riskId}/incidents/{incident.Id}",
            IncidentResponse.FromIncident(incident));
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid riskId,
        Guid incidentId,
        UpdateIncidentStatusRequest request,
        IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["status"] =
                    [
                        "Status is invalid."
                    ]
                });
        }

        var result =
            await repository.UpdateStatusAsync(
                riskId,
                incidentId,
                request.Status,
                cancellationToken);

        return result.Outcome switch
        {
            IncidentStatusUpdateOutcome.NotFound =>
                Results.NotFound(),
            IncidentStatusUpdateOutcome.InvalidTransition
                or IncidentStatusUpdateOutcome.Conflict =>
                Results.Conflict(
                    new IncidentTransitionErrorResponse(
                        result.Error!)),
            _ => Results.Ok(
                IncidentResponse.FromIncident(
                    result.Incident!))
        };
    }

    private static Dictionary<string, string[]> Validate(
        CreateIncidentRequest request)
    {
        var errors =
            new Dictionary<string, string[]>();

        ValidateRequiredText(
            errors,
            fieldName: "title",
            displayName: "Title",
            request.Title,
            MaximumTitleLength);

        ValidateRequiredText(
            errors,
            fieldName: "description",
            displayName: "Description",
            request.Description,
            MaximumDescriptionLength);

        ValidateRequiredText(
            errors,
            fieldName: "responsiblePerson",
            displayName: "Responsible person",
            request.ResponsiblePerson,
            MaximumResponsiblePersonLength);

        var distinctMitigationIds =
            request.MitigationIds?
                .Distinct()
                .ToArray()
            ?? [];

        if (
            distinctMitigationIds.Any(
                mitigationId =>
                    mitigationId == Guid.Empty))
        {
            errors["mitigationIds"] =
            [
                "Mitigation ids cannot be empty."
            ];
        }
        else if (
            distinctMitigationIds.Length >
            MaximumMitigationLinks)
        {
            errors["mitigationIds"] =
            [
                $"An incident cannot link more than {MaximumMitigationLinks} mitigation actions."
            ];
        }

        return errors;
    }

    private static void ValidateRequiredText(
        Dictionary<string, string[]> errors,
        string fieldName,
        string displayName,
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] =
            [
                $"{displayName} is required."
            ];
        }
        else if (value.Trim().Length > maximumLength)
        {
            errors[fieldName] =
            [
                $"{displayName} cannot exceed {maximumLength} characters."
            ];
        }
    }
}

public sealed record IncidentTransitionErrorResponse(
    string Error);
