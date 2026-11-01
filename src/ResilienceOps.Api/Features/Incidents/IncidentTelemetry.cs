using System.Diagnostics;

namespace ResilienceOps.Api.Features.Incidents;

public static class IncidentTelemetry
{
    public const string ActivitySourceName =
        "ResilienceOps.Api.Incidents";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}
