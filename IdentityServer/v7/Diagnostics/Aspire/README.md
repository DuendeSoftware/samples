# Diagnostics with Aspire dashboard sample

This sample shows how to instrument Duende IdentityServer with
[OpenTelemetry](https://docs.duendesoftware.com/identityserver/observability/) so that traces,
metrics and logs flow into a central backend — here, the .NET Aspire dashboard, which ships with a
built-in OpenTelemetry collector and viewer.

It also demonstrates the Aspire service defaults pattern: a shared `Aspire.ServiceDefaults` project
wiring up service discovery, resilience, health checks and telemetry once, so each service only has
to call `builder.AddServiceDefaults()`.

### What the sample demonstrates

- a shared `Aspire.ServiceDefaults` project providing:
  - `AddServiceDiscovery()` and `AddStandardResilienceHandler()` on `HttpClient`
  - `AddOpenTelemetry()` for traces, metrics and logs, with the OTLP exporter wired to the Aspire
    dashboard
  - `AddMeter("Duende.IdentityServer.Experimental")`, which subscribes to the counters Duende
    IdentityServer itself publishes
  - `AddHealthChecks()` plus `MapDefaultEndpoints()`, exposing `/health` and `/alive`
- a local `Aspire.ServiceDefaults` copy (rather than the shared one at the v7 root) so the wiring is
  visible in a single place
- **custom application metrics** in `IdentityServer/Pages/Telemetry.cs` — a `System.Diagnostics.Metrics`
  `Meter` with four counters, recorded from the UI code that a real application owns:
  - `tokenservice.user_login`, tagged with the client id and identity provider
  - `tokenservice.consent`, tagged with client, scope, remember flag and granted/denied
  - `tokenservice.grants_revoked`
  - `tokenservice.user_logout`
- the calls to those counters from the login, consent, external-login callback and grants pages
- Aspire's service discovery: `Aspire.Web` reads its authority from
  `builder.Configuration["services:identityserver:https:0"]` and calls the API at
  `https://apiservice` rather than a hard-coded host and port
- a three-tier topology: `Aspire.Web` (Razor Pages frontend) → `Aspire.ApiService` (protected API) →
  `IdentityServer`

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project Aspire.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:15264` and shows four resources: `identityserver`,
   `apiservice`, `webfrontend` and the `Aspire.AppHost` orchestrator.

1. Open a browser tab to the `webfrontend` at `https://localhost:5014`. You are redirected to the
   IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` and approve the consent request. The web app then calls the API
   service and renders the weather forecast.
1. Go back to the Aspire dashboard and open the **Metrics** view for the `IdentityServer` resource.
   Search for `tokenservice` and you should see the counters your login and consent just recorded.
1. Open the **Traces** view and look at the waterfall for the sign-in request. You can see the
   `Duende.IdentityServer` activity spans, the outgoing HTTP calls from `Aspire.Web` to
   `apiservice`, and the validation steps inside the token endpoint.
1. Open the **Logs** view on `IdentityServer` to see structured log records correlated by trace id.
1. Check `/health` and `/alive` on any resource to see the health checks that `AddServiceDefaults`
   registered.
1. Trigger a new metric by revoking a grant on the **Grants** page, then refresh the Metrics view to
   see `tokenservice.grants_revoked` increment.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `Aspire.AppHost` | `https://localhost:15264` | Aspire dashboard, orchestration and topology |
| `IdentityServer` | `https://localhost:5001` | Token service; publishes the custom `tokenservice.*` metrics |
| `Aspire.ApiService` | `https://localhost:5325` | Protected weather API |
| `Aspire.Web` | `https://localhost:5014` | Razor Pages frontend, calls the API |
| `Aspire.ServiceDefaults` | _(library)_ | Shared telemetry, discovery, resilience and health checks |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

No database or seeding step is required — the configuration is in-memory and the test users are
registered with `AddTestUsers`.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `Aspire.ServiceDefaults/Extensions.cs` — the entire telemetry, discovery and health check wiring
- `IdentityServer/Pages/Telemetry.cs` — the custom `Meter` and the four counters
- `IdentityServer/Pages/Account/Login/Index.cshtml.cs` — where `UserLogin` is recorded
- `IdentityServer/Pages/Consent/Index.cshtml.cs` — where `ConsentGranted` / `ConsentDenied` are recorded
- `Aspire.AppHost/Program.cs` — the resource graph and `.WithReference(...)` wiring
- `Aspire.Web/Program.cs` — service discovery via `Configuration["services:identityserver:https:0"]`

For a plain console/OpenTelemetry setup with no Aspire, see the sibling
[`Otel`](../Otel) sample.
