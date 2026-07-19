using System.Diagnostics;

namespace ResilienceOps.Api.Features.Risks;

public static class RiskTelemetry
{
    public const string ActivitySourceName =
        "ResilienceOps.Api.Risks";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}