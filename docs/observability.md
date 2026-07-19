# Observability

ResilienceOps uses OpenTelemetry with the Azure Monitor distribution for
Application Insights integration.

## Collected telemetry

The API is prepared to collect:

- ASP.NET Core request telemetry
- outgoing HTTP dependencies
- exceptions
- runtime metrics
- structured application logs
- custom activities from the risk feature
- database dependency information

## Service identity

The OpenTelemetry service name is:

```text
ResilienceOps.Api
```

This provides a stable service identity in Application Insights and the
Application Map.

## Local development

Azure Monitor export is disabled when no Application Insights connection
string is configured. Console logging remains enabled.

The connection string must never be committed to Git.

For a temporary PowerShell session:

```powershell
$env:APPLICATIONINSIGHTS_CONNECTION_STRING = "<connection-string>"
```

Remove it again with:

```powershell
Remove-Item Env:APPLICATIONINSIGHTS_CONNECTION_STRING
```

## Useful Application Insights queries

### Risk creation logs

```kusto
traces
| where message contains "Persisted operational risk"
| order by timestamp desc
```

### API request performance

```kusto
requests
| where url contains "/api/risks"
| summarize
    RequestCount = count(),
    AverageDuration = avg(duration),
    SlowestRequest = max(duration)
    by name
```

### Failed requests

```kusto
requests
| where success == false
| order by timestamp desc
```

### Exceptions

```kusto
exceptions
| order by timestamp desc
```

### Dependencies

```kusto
dependencies
| summarize
    DependencyCount = count(),
    AverageDuration = avg(duration)
    by type, target, name
```

## Data minimization

Risk titles and descriptions are not written to logs or telemetry.
Telemetry includes identifiers, severity, status, timing and technical
diagnostic information.