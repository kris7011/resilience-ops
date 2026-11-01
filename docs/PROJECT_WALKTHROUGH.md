# ResilienceOps — project walkthrough

A developer-level guide to how the application works, why it is built this way and how it is verified. It describes only what exists in the repository. Items marked **planned** are not implemented.

## 1. System overview

ResilienceOps manages operational risks, the mitigation actions that reduce them, and the incidents in which a risk actually materialised.

```text
Angular 21 SPA (localhost:4200)
        │  HttpClient, JSON
        ▼
ASP.NET Core 10 Minimal API (localhost:5014)
        │  EF Core 10
        ▼
SQLite file  src/ResilienceOps.Api/Data/resilience-ops.db
```

| Concept | What it holds | Lifecycle |
| --- | --- | --- |
| Risk (`RiskItem`) | title, description, severity | Open → Mitigating → Closed → (reopen) Open |
| Mitigation action (`MitigationAction`) | description, owner, due date | Planned → InProgress → Completed → (reopen) Planned |
| Incident (`Incident`) | title, description, responsible person, linked mitigation actions, status history | Reported → Investigating → Resolved → Closed; Resolved/Closed can be reopened to Investigating |

## 2. Architecture and structure

- **Feature folders** in the API: `Features/Risks`, `Features/Mitigations`, `Features/Incidents`. Each has a domain entity, request/response records, an endpoint mapping class, a repository interface and an EF Core repository.
- **Minimal APIs** grouped under `/api/risks/{riskId}/...`, so every mitigation action and incident is addressed through its risk. A child of another risk is simply not found (`404`).
- **Rich domain entities**: private setters and factory methods. Business rules (the incident lifecycle, timestamps, history) live in `Incident`, not in the endpoint.
- **Repository abstraction** (`IIncidentRepository`) keeps EF Core out of the endpoints and is where persistence concerns such as concurrency are handled.
- **Angular**: standalone components, Signals for state, Reactive Forms, one `HttpClient` service per feature (`IncidentApiService`, `MitigationApiService`, `RiskApiService`).

## 3. Incident management in detail

### Problem

A risk register says what *could* go wrong. Teams also need to record when it *did* go wrong: who is responsible, which mitigation actions were relevant, and how the incident progressed from report to closure.

### Domain model — `src/ResilienceOps.Api/Features/Incidents`

- `Incident` is the aggregate root. `Incident.Create(...)` validates required text, trims it, de-duplicates mitigation ids, sets `Status = Reported` and appends the first history entry.
- `Incident.ChangeStatus(newStatus)`:
  1. rejects undefined enum values;
  2. rejects a move to the current status ("The incident is already Closed.");
  3. rejects any move not in `CanTransitionTo` (for example Reported → Closed);
  4. sets the matching timestamp (`InvestigatingUtc`, `ResolvedUtc`, `ClosedUtc`) and `UpdatedUtc`; reopening clears `ResolvedUtc` and `ClosedUtc`;
  5. appends an `IncidentHistoryEntry` with `Sequence = history.Count + 1`, previous and new status.
- Rule violations throw `InvalidOperationException`; the repository turns that into an `InvalidTransition` outcome and the endpoint into `409 Conflict` with `{ "error": "<reason>" }`.
- Timestamps are truncated to whole milliseconds because they are stored as Unix milliseconds. Without that, the `201` response and a later `GET` would show different times.

### Persistence — `ResilienceOpsDbContext` and migration `AddIncidents`

| Table | Key | Notes |
| --- | --- | --- |
| `Incidents` | `Id` | FK to `Risks` (cascade); indexes `(RiskId, Status)` and `(RiskId, ReportedUtc)` |
| `IncidentHistory` | `Id` | FK to `Incidents` (cascade); **unique** `(IncidentId, Sequence)` |
| `IncidentMitigations` | `(IncidentId, MitigationId)` | FK to `Incidents` (cascade) and `MitigationActions` (restrict) |

- The migration only creates tables. Upgrading an existing database keeps its risks and mitigation actions; this was demonstrated by creating a database with the previous release and starting the new one on it.
- `IncidentHistoryEntry.Id` is configured with `ValueGeneratedNever()`. The domain assigns the Guid. Without this setting EF Core treats a new entry found through a loaded incident's navigation as an *existing* row and issues an `UPDATE`, which affects zero rows and fails every status change with `DbUpdateConcurrencyException`. The integration tests caught this.
- The link to mitigation actions is `Restrict` rather than `Cascade` so the schema has a single cascade path from a risk. SQL Server (the planned Azure SQL target) rejects multiple cascade paths. There is no endpoint that deletes risks or mitigation actions today.

### Concurrency

Two requests can load the same incident and both try to append history entry *n + 1*. The unique `(IncidentId, Sequence)` index lets only the first commit. `EfCoreIncidentRepository.UpdateStatusAsync` catches the resulting `DbUpdateException`, logs a warning and returns a `Conflict` outcome, so the client receives `409` ("The incident was changed by another request. Reload it and try again.") instead of `500`. This is optimistic concurrency without a separate row-version column.

### Request flow — change status

```text
IncidentPanel.changeIncidentStatus()
  → IncidentApiService.updateIncidentStatus()      PATCH /api/risks/{riskId}/incidents/{id}/status
    → IncidentEndpoints.UpdateStatusAsync()        400 if the enum value is undefined
      → EfCoreIncidentRepository.UpdateStatusAsync()
          load incident + history (tracked, scoped to riskId)   → NotFound → 404
          Incident.ChangeStatus()                                → InvalidTransition → 409
          SaveChangesAsync()                                     → DbUpdateException → Conflict → 409
      ← 200 IncidentResponse (with ordered history)
  ← panel replaces the card; on 409 it shows the reason and reloads the stored incident
```

### Validation

| Rule | Where | Response |
| --- | --- | --- |
| title ≤ 120, description ≤ 1000, responsible person ≤ 120, all required | `IncidentEndpoints.Validate` (and the Angular form) | `400` ValidationProblem |
| at most 50 linked mitigation actions, no empty ids | `IncidentEndpoints.Validate` | `400` |
| linked mitigation actions belong to the same risk | `IncidentEndpoints.CreateAsync` | `400` (`mitigationIds`) |
| risk exists | `CreateAsync` / `GetAllAsync` | `404` |
| allowed lifecycle move | `Incident.ChangeStatus` | `409` |

### Frontend — `src/resilience-ops-web/src/app/features/incidents`

- `IncidentPanel` loads incidents and the risk's mitigation actions together (`forkJoin`) so linked actions can be shown by description and offered as checkboxes.
- Status buttons come from a table (`incidentStatusActions`) that mirrors the API lifecycle. The UI never offers a move the API would reject, but the API remains the authority.
- Errors: `409` shows the API's reason; `400` shows the field messages; other failures show a generic message.
- The dashboard (`app.html`) adds a "Manage incidents" toggle per risk next to "Manage mitigation actions".

### Observability

`IncidentTelemetry` (`ResilienceOps.Api.Incidents`) traces repository operations with tags such as `incident.id`, `incident.requested_status` and `incident.transition_valid`. See [observability.md](observability.md).

## 4. Authentication and authorization

**Not implemented.** The API is open and intended for local use. Authentication, authorization and per-tenant isolation are planned. The responsible person is therefore free text, not a user account.

## 5. Testing

| Suite | Location | What it proves |
| --- | --- | --- |
| API integration tests (xUnit, 30) | `tests/ResilienceOps.Api.IntegrationTests` | The real API host and real migrations on in-memory SQLite. 12 incident tests: creation (trimming, de-duplicated links, `Location` header, first history entry), persistence, unknown risk, validation, cross-risk mitigation links, full lifecycle with ordered history and cleared timestamps, reopen from Resolved, skipped step and same-status `409`, cross-risk `404`, undefined status `400`, and a concurrency race. |
| Angular unit tests (Vitest, 15) | `src/resilience-ops-web/src/app/**/*.spec.ts` | 7 incident panel tests with mocked services: rendering of owner, linked actions and timeline; available actions per status; registration with selected links; blocked invalid form; status change; `409` message plus reload; load failure. |

The concurrency test (`IncidentConcurrencyTests`) uses a `SaveChangesInterceptor` to commit a competing change through a second `DbContext` between the repository's load and save. Removing the `catch` in the repository makes this test fail with the unique-index violation, so it is not a false positive.

Mocked: the Angular tests mock the HTTP services. Not mocked: the API tests use the real endpoints, EF Core and SQLite.

## 6. Local development and demonstration

```bash
dotnet run --project src/ResilienceOps.Api --launch-profile http   # http://localhost:5014
cd src/resilience-ops-web && npm ci && npm start                    # http://localhost:4200
```

To try incidents: open a risk, choose **Manage incidents**, register an incident, optionally link a mitigation action, then use the status buttons. The timeline grows with each change and survives a page reload.

Demonstrated on 1 November 2026 (Windows 11, Edge via Playwright, real API and SQLite file):

1. A database was created with the previous release (`main` before this feature) and two mitigation actions were added.
2. The new release started on that file, applied `AddIncidents`, and kept both mitigation actions.
3. In the UI an incident was registered with a linked action and moved Reported → Investigating → Resolved.
4. Another client closed it through the API; the stale UI's "Close incident" got `409` "The incident is already Closed." and the card refreshed to Closed.
5. After a reload the incident, its four timeline entries and the linked action were still shown; reopening moved it to Investigating.
6. At 390 px width the page had no horizontal overflow.

Screenshots: [lifecycle](images/incident-lifecycle.png), [conflict](images/incident-conflict.png), [phone width](images/incident-mobile.png).

## 7. Backup and restore

The database is one SQLite file (`Data/resilience-ops.db`, with `-wal`/`-shm` side files while the API runs). Stop the API, or run `PRAGMA wal_checkpoint(TRUNCATE)`, before copying it; a copy of only the `.db` file taken while the API runs can miss recent writes that are still in the `-wal` file.

## 8. Design decisions and trade-offs

| Decision | Alternatives | Why |
| --- | --- | --- |
| Incidents nested under risks | Top-level `/api/incidents` | Matches the existing mitigation design; every query is scoped by risk, which makes cross-risk access a `404` by construction. A cross-risk incident list would need a new endpoint. |
| Explicit state machine in the entity | Free status changes (as mitigation actions allow) | Incidents need a trustworthy timeline; skipping Investigating or closing an unresolved incident would make it meaningless. |
| Append-only history table | Only current status + timestamps | Reopening overwrites timestamps; the history keeps every step. |
| Unique `(IncidentId, Sequence)` as the concurrency guard | Row-version column, pessimistic locking | No extra column, and it protects exactly the invariant that matters (no duplicate or lost steps). A row-version would also catch concurrent edits of other fields, which do not exist yet. |
| `409` for lifecycle violations | `400` | The request is well-formed; it conflicts with the current state of the resource. |

## 9. Known limitations and future work

- The responsible person and the linked mitigation actions are set at registration and cannot be changed yet; the history records status changes only.
- No authentication or authorization (planned).
- Risk and mitigation timestamps are still returned with sub-millisecond precision on create (pre-existing behaviour; incidents truncate).
- The API base URL is hard-coded to `http://localhost:5014` in the Angular services (planned: per-environment configuration).
- No end-to-end test suite in CI; the browser demonstration above was run manually with Playwright.

## 10. Ownership and AI assistance

The incident feature was implemented with AI assistance. A first draft of the backend model, repository and endpoints was written by Codex in an interrupted session and was never applied or tested. Claude Code applied it, found and fixed the defects described above (history inserts, timestamp precision, concurrency `500`, telemetry naming), added the reopen-from-Resolved transition, the migration, all tests, the Angular panel, the documentation and the demonstration. The owner reviews and merges.

## 11. Interview preparation — incident management

**Non-technical:** When something goes wrong, the team records it against the risk it belongs to, names who is in charge, notes which planned countermeasures were involved, and moves it from "reported" to "closed". Every step is time-stamped so the story can be reviewed later.

**Developer level:** `Incident` is an aggregate with a small state machine. Each accepted change appends a history row. The API is a Minimal API group under the risk; EF Core persists the aggregate with two child tables. Invalid moves give `409`, bad input `400`, wrong risk `404`.

**Senior / architectural:** Business rules sit in the domain entity and are enforced server-side; the UI mirrors them only for usability. Consistency under concurrency comes from a unique index on the history sequence (optimistic concurrency), translated into a `409` at the repository boundary. The migration is additive, so deployment is backwards-compatible with existing data. One cascade path keeps the schema portable to SQL Server.

**Questions and answers**

1. *Why does `IncidentHistoryEntry.Id` need `ValueGeneratedNever()`?* Guid keys are value-generated by convention. When EF Core discovers a new entity through a navigation and its key is already set, it assumes the row exists and issues an `UPDATE`. Telling EF the key is never generated makes it `INSERT`. The integration tests caught this as a `DbUpdateConcurrencyException`.
2. *How do you stop two users from corrupting the timeline?* Both would append the same sequence number; the unique index rejects the second insert, and the repository maps the `DbUpdateException` to `409`. The test proves it by injecting a competing commit between load and save.
3. *Why `409` and not `400` for Reported → Closed?* The body is valid; the conflict is with the resource's current state. Clients can reload and retry with a legal move.
4. *Why put incidents under `/api/risks/{riskId}`?* It matches the mitigation design and scopes every read and write by risk. The trade-off is that a global incident list would need another endpoint.
5. *What would you add for production?* Authentication and per-user responsibility, reassignment with history entries, configurable API URLs, Azure SQL with the same migrations, and end-to-end tests in CI.

**Hands-on exercise:** Add `PATCH /api/risks/{riskId}/incidents/{id}/responsible-person` that reassigns the incident and records the change in the history. Extend `IncidentHistoryEntry` with an event type, add a migration, cover it with an integration test, and show the reassignment in the panel's timeline.
