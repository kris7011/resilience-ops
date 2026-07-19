using System.Diagnostics;

namespace ResilienceOps.Api.Features.Mitigations;

public static class MitigationTelemetry
{
    public const string ActivitySourceName =
        "ResilienceOps.Api.Mitigations";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}